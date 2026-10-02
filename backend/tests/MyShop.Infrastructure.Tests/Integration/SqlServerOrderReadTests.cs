using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerOrderReadTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task ListsNewestFirstAndReturnsTheRecordedOrderSnapshot()
    {
        var older = CreateOrder(DateTimeOffset.UtcNow.AddMinutes(-1), "First customer");
        var newer = CreateOrder(DateTimeOffset.UtcNow, "Second customer",
            "Betaal binnen 14 dagen onder vermelding van het bestelnummer.");
        var refundable = CreateOrder(DateTimeOffset.UtcNow.AddMinutes(-2), "Refund customer");
        var owned = CreateOrder(DateTimeOffset.UtcNow.AddMinutes(1), "Account customer");
        var customerUserId = Guid.NewGuid().ToString();
        await using (var writeContext = database.CreateContext())
        {
            writeContext.Users.Add(new IdentityUser { Id = customerUserId, UserName = "customer@example.test" });
            await writeContext.SaveChangesAsync();
            var writer = new OrderRepository(writeContext);
            await writer.AddAsync(older, Guid.NewGuid(), null, [], CancellationToken.None);
            await writer.AddAsync(newer, Guid.NewGuid(), null, [], CancellationToken.None);
            await writer.AddAsync(refundable, Guid.NewGuid(), null, [], CancellationToken.None);
            await writer.AddAsync(owned, Guid.NewGuid(), customerUserId, [], CancellationToken.None);
        }

        await using var readContext = database.CreateContext();
        var repository = new OrderRepository(readContext);
        var customerPage = await repository.ListAsync(customerUserId, 0, 20, CancellationToken.None);
        Assert.Equal(1, customerPage.TotalCount);
        Assert.Equal(owned.Id, Assert.Single(customerPage.Items).Id);
        var ownedDetail = await repository.GetAsync(customerUserId, owned.Id, CancellationToken.None);
        Assert.Equal(owned.Id, ownedDetail!.Id);
        Assert.Null(await repository.GetAsync("another-customer", owned.Id, CancellationToken.None));
        Assert.Null(await repository.GetAsync(customerUserId, newer.Id, CancellationToken.None));
        Assert.Null(await repository.CancelAsync("another-customer", owned.Id, ownedDetail.Revision,
            DateTimeOffset.UtcNow, "Must not cancel", CancellationToken.None));
        Assert.Equal(OrderStatus.AwaitingPayment,
            (await repository.GetAsync(customerUserId, owned.Id, CancellationToken.None))!.Status);
        var page = await repository.ListAsync(0, 100, null, null, CancellationToken.None);
        var olderIndex = page.Items.ToList().FindIndex(item => item.Id == older.Id);
        var newerIndex = page.Items.ToList().FindIndex(item => item.Id == newer.Id);
        Assert.True(newerIndex >= 0 && olderIndex > newerIndex);
        Assert.Equal("Second customer", page.Items[newerIndex].CustomerName);
        Assert.Equal(20m, Assert.Single(page.Items[newerIndex].Totals).Amount);

        var detail = await repository.GetAsync(newer.Id, CancellationToken.None);
        Assert.NotNull(detail);
        Assert.Equal("customer@example.test", detail.Email);
        Assert.Equal("Betaal binnen 14 dagen onder vermelding van het bestelnummer.",
            detail.PaymentInstructions);
        Assert.Equal("Teststraat 1", detail.AddressLine);
        Assert.Equal("Shirt", Assert.Single(detail.Lines).ProductName);
        Assert.Equal(2, detail.Lines[0].Quantity);
        Assert.Equal(10m, detail.Lines[0].UnitAmount);
        Assert.NotEqual(Guid.Empty, detail.Revision);

        var paidAt = DateTimeOffset.UtcNow;
        var updated = await repository.MarkPaidAsync(newer.Id, detail.Revision, paidAt,
            "bankafschrift 12345", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Null(await repository.MarkPaidAsync(newer.Id, detail.Revision, paidAt,
            "duplicate", CancellationToken.None));
        var paid = await repository.GetAsync(newer.Id, CancellationToken.None);
        Assert.Equal(OrderStatus.Paid, paid!.Status);
        Assert.Equal(paidAt.ToUniversalTime(), paid.PaidAt);
        Assert.Equal("bankafschrift 12345", paid.PaymentReference);
        Assert.Equal(updated.Value, paid.Revision);

        var shippedAt = DateTimeOffset.UtcNow;
        var shipped = await repository.MarkShippedAsync(newer.Id, paid.Revision,
            shippedAt, "PostNL", "3SMYSHOP123", CancellationToken.None);
        Assert.NotNull(shipped);
        Assert.Null(await repository.MarkShippedAsync(newer.Id, paid.Revision,
            shippedAt, "DHL", "DUPLICATE", CancellationToken.None));
        var persistedShipment = await repository.GetAsync(newer.Id, CancellationToken.None);
        Assert.Equal(OrderStatus.Shipped, persistedShipment!.Status);
        Assert.Equal(paidAt.ToUniversalTime(), persistedShipment.PaidAt);
        Assert.Equal("bankafschrift 12345", persistedShipment.PaymentReference);
        Assert.Equal(shippedAt.ToUniversalTime(), persistedShipment.ShippedAt);
        Assert.Equal("PostNL", persistedShipment.ShippingCarrier);
        Assert.Equal("3SMYSHOP123", persistedShipment.TrackingCode);
        Assert.Equal(shipped.Value, persistedShipment.Revision);

        var awaitingCancellation = await repository.GetAsync(older.Id, CancellationToken.None);
        var cancelledAt = DateTimeOffset.UtcNow;
        var cancelledRevision = await repository.CancelAsync(older.Id, awaitingCancellation!.Revision,
            cancelledAt, "Klant ziet af van bestelling.", CancellationToken.None);
        Assert.NotNull(cancelledRevision);
        Assert.Null(await repository.CancelAsync(older.Id, awaitingCancellation.Revision,
            cancelledAt, "Tweede poging", CancellationToken.None));
        var cancelled = await repository.GetAsync(older.Id, CancellationToken.None);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
        Assert.Equal(cancelledAt.ToUniversalTime(), cancelled.CancelledAt);
        Assert.Equal("Klant ziet af van bestelling.", cancelled.CancellationReason);
        Assert.Equal(cancelledRevision.Value, cancelled.Revision);

        var awaitingRefund = await repository.GetAsync(refundable.Id, CancellationToken.None);
        var refundPaidAt = DateTimeOffset.UtcNow;
        var refundPaidRevision = await repository.MarkPaidAsync(refundable.Id, awaitingRefund!.Revision,
            refundPaidAt, "bankafschrift 54321", CancellationToken.None);
        Assert.NotNull(refundPaidRevision);
        var refundedAt = DateTimeOffset.UtcNow;
        var refundedRevision = await repository.RefundAsync(refundable.Id, refundPaidRevision!.Value,
            refundedAt, "bankafschrift 67890", "Dubbele betaling.", CancellationToken.None);
        Assert.NotNull(refundedRevision);
        Assert.Null(await repository.RefundAsync(refundable.Id, refundPaidRevision.Value,
            refundedAt, "duplicate", "Tweede poging", CancellationToken.None));
        var refunded = await repository.GetAsync(refundable.Id, CancellationToken.None);
        Assert.Equal(OrderStatus.Refunded, refunded!.Status);
        Assert.Equal(refundPaidAt.ToUniversalTime(), refunded.PaidAt);
        Assert.Equal("bankafschrift 54321", refunded.PaymentReference);
        Assert.Equal(refundedAt.ToUniversalTime(), refunded.RefundedAt);
        Assert.Equal("bankafschrift 67890", refunded.RefundReference);
        Assert.Equal("Dubbele betaling.", refunded.RefundReason);
        Assert.Equal(refundedRevision.Value, refunded.Revision);

        var shippedPage = await repository.ListAsync(0, 100, OrderStatus.Shipped, "Second",
            CancellationToken.None);
        Assert.Equal(1, shippedPage.TotalCount);
        Assert.Equal(newer.Id, Assert.Single(shippedPage.Items).Id);
        var cancelledPage = await repository.ListAsync(0, 100, OrderStatus.Cancelled, null,
            CancellationToken.None);
        Assert.Equal(1, cancelledPage.TotalCount);
        Assert.Equal(older.Id, Assert.Single(cancelledPage.Items).Id);
        var refundedPage = await repository.ListAsync(0, 100, OrderStatus.Refunded, null,
            CancellationToken.None);
        Assert.Equal(refundable.Id, Assert.Single(refundedPage.Items).Id);
        Assert.Equal(1, (await repository.ListAsync(0, 100, null, newer.Number,
            CancellationToken.None)).TotalCount);
        var emailPage = await repository.ListAsync(0, 100, null, "customer@example.test",
            CancellationToken.None);
        Assert.Contains(emailPage.Items, item => item.Id == older.Id);
        Assert.Contains(emailPage.Items, item => item.Id == newer.Id);
        Assert.Null(await repository.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [SqlServerFact]
    public async Task AddPaidAsyncCreatesPaidOnlineOrder()
    {
        var order = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow,
            OrderCustomer.Create("Online customer", "online@example.test"),
            DeliveryAddress.Create("Betaalstraat 1", "1234 AB", "Utrecht", "NL"),
            [(Guid.NewGuid(), Guid.NewGuid(), "Shirt", "Blauw", 2, Money.Create(10m, "EUR"))],
            paymentMethod: OrderPaymentMethod.Online);
        var checkoutToken = Guid.NewGuid();

        await using (var writeContext = database.CreateContext())
        {
            var writer = new OrderRepository(writeContext);
            var receipt = await writer.AddPaidAsync(order, checkoutToken, "MSP-123", [],
                CancellationToken.None);
            Assert.NotNull(receipt);
        }

        await using var readContext = database.CreateContext();
        var repository = new OrderRepository(readContext);
        var detail = await repository.GetAsync(order.Id, CancellationToken.None);
        Assert.NotNull(detail);
        Assert.Equal(OrderPaymentMethod.Online, detail.PaymentMethod);
        Assert.Equal(OrderStatus.Paid, detail.Status);
        Assert.NotNull(detail.PaidAt);
        Assert.Equal("MSP-123", detail.PaymentReference);
        Assert.Null(detail.PaymentInstructions);
        Assert.Equal(20m, Assert.Single(detail.Totals).Amount);
        Assert.Equal(order.Id, (await repository.GetByCheckoutTokenAsync(checkoutToken,
            CancellationToken.None))!.Id);
    }

    private static Order CreateOrder(DateTimeOffset placedAt, string customerName,
        string? paymentInstructions = null) => Order.Place(
        Guid.NewGuid(),
        placedAt,
        OrderCustomer.Create(customerName, "customer@example.test"),
        DeliveryAddress.Create("Teststraat 1", "1234 AB", "Utrecht", "NL"),
        [(Guid.NewGuid(), Guid.NewGuid(), "Shirt", "Blauw", 2, Money.Create(10m, "EUR"))],
        paymentInstructions);
}
