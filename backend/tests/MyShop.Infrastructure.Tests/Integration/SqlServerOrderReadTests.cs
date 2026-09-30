using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerOrderReadTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task ListsNewestFirstAndReturnsTheRecordedOrderSnapshot()
    {
        var older = CreateOrder(DateTimeOffset.UtcNow.AddMinutes(-1), "First customer");
        var newer = CreateOrder(DateTimeOffset.UtcNow, "Second customer");
        await using (var writeContext = database.CreateContext())
        {
            var writer = new OrderRepository(writeContext);
            await writer.AddAsync(older, Guid.NewGuid(), CancellationToken.None);
            await writer.AddAsync(newer, Guid.NewGuid(), CancellationToken.None);
        }

        await using var readContext = database.CreateContext();
        var repository = new OrderRepository(readContext);
        var page = await repository.ListAsync(0, 100, null, null, CancellationToken.None);
        var olderIndex = page.Items.ToList().FindIndex(item => item.Id == older.Id);
        var newerIndex = page.Items.ToList().FindIndex(item => item.Id == newer.Id);
        Assert.True(newerIndex >= 0 && olderIndex > newerIndex);
        Assert.Equal("Second customer", page.Items[newerIndex].CustomerName);
        Assert.Equal(20m, Assert.Single(page.Items[newerIndex].Totals).Amount);

        var detail = await repository.GetAsync(newer.Id, CancellationToken.None);
        Assert.NotNull(detail);
        Assert.Equal("customer@example.test", detail.Email);
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

        var shippedPage = await repository.ListAsync(0, 100, OrderStatus.Shipped, "Second",
            CancellationToken.None);
        Assert.Equal(1, shippedPage.TotalCount);
        Assert.Equal(newer.Id, Assert.Single(shippedPage.Items).Id);
        var cancelledPage = await repository.ListAsync(0, 100, OrderStatus.Cancelled, null,
            CancellationToken.None);
        Assert.Equal(1, cancelledPage.TotalCount);
        Assert.Equal(older.Id, Assert.Single(cancelledPage.Items).Id);
        Assert.Equal(1, (await repository.ListAsync(0, 100, null, newer.Number,
            CancellationToken.None)).TotalCount);
        var emailPage = await repository.ListAsync(0, 100, null, "customer@example.test",
            CancellationToken.None);
        Assert.Contains(emailPage.Items, item => item.Id == older.Id);
        Assert.Contains(emailPage.Items, item => item.Id == newer.Id);
        Assert.Null(await repository.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private static Order CreateOrder(DateTimeOffset placedAt, string customerName) => Order.Place(
        Guid.NewGuid(),
        placedAt,
        OrderCustomer.Create(customerName, "customer@example.test"),
        DeliveryAddress.Create("Teststraat 1", "1234 AB", "Utrecht", "NL"),
        [(Guid.NewGuid(), Guid.NewGuid(), "Shirt", "Blauw", 2, Money.Create(10m, "EUR"))]);
}
