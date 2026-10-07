using Microsoft.EntityFrameworkCore;
using MyShop.Application.Billing;
using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Persistence.Repositories;
namespace MyShop.Infrastructure.Tests.Integration;
[Collection(SqlServerCollection.Name)]
public sealed class BillingInvoiceStorageTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task Issues_once_keeps_snapshots_and_enforces_customer_ownership()
    {
        await using var context = database.CreateContext();
        var repository = new BillingRepository(context);
        var settings = new BillingSettings(repository);
        var original = await repository.GetCompanyAsync(CancellationToken.None);
        var savedCompany = (await settings.SaveCompanyAsync(original with { Name = "Seller BV", AddressLine = "Street 1", PostalCode = "1234 AB", City = "Utrecht", VatId = "NL123456789B01", KvkNumber = "12345678" }, CancellationToken.None))!;
        var customerId = Guid.NewGuid().ToString();
        context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser { Id = customerId, UserName = customerId, NormalizedUserName = customerId.ToUpperInvariant() });
        await context.SaveChangesAsync();
        var product = Guid.NewGuid(); var variant = Guid.NewGuid();
        var order = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow, OrderCustomer.Create("Buyer", "buyer@example.test"),
            DeliveryAddress.Create("Buyer street 2", "2345 BC", "Amsterdam", "NL"),
            [(product, variant, "Product", "Variant", 2, Money.Create(121, "EUR"))],
            vatRates: new Dictionary<(Guid, Guid), (decimal, bool)> { [(product, variant)] = (21, false) });
        var orders = new OrderRepository(context);
        await orders.AddAsync(order, Guid.NewGuid(), customerId, [], CancellationToken.None);
        var detail = (await orders.GetAsync(order.Id, CancellationToken.None))!;
        var service = new IssueInvoice(repository, orders, repository);
        var command = new IssueInvoiceCommand(order.Id, detail.Revision, DateOnly.FromDateTime(DateTime.UtcNow),
            new("Buyer BV", "Buyer street 2", "2345 BC", "Amsterdam", "NL", "NL987654321B01"), true, "");
        await using var secondContext = database.CreateContext();
        var secondRepository = new BillingRepository(secondContext);
        var results = await Task.WhenAll(service.ExecuteAsync(command, CancellationToken.None),
            new IssueInvoice(secondRepository, new OrderRepository(secondContext), secondRepository).ExecuteAsync(command, CancellationToken.None));
        var invoice = results[0]!;
        Assert.NotNull(results[1]); Assert.Equal(invoice.Number, results[1]!.Number);
        Assert.StartsWith("INV-", invoice.Number); Assert.Equal("200.00", invoice.Net); Assert.Equal("42.00", invoice.Vat); Assert.Equal("242.00", invoice.Gross);
        Assert.Equal(invoice.Number, (await service.ExecuteAsync(command, CancellationToken.None))!.Number);
        Assert.NotNull(await repository.GetAsync(order.Id, customerId, CancellationToken.None));
        Assert.Null(await repository.GetAsync(order.Id, "someone-else", CancellationToken.None));
        await settings.SaveCompanyAsync(savedCompany with { Name = "New seller name" }, CancellationToken.None);
        var retrieved = (await repository.GetAsync(order.Id, null, CancellationToken.None))!;
        Assert.Equal("Seller BV", retrieved.Seller.Name); Assert.Equal("Buyer BV", retrieved.Buyer.Name);
        Assert.Equal(1, await context.Invoices.CountAsync(row => row.OrderId == order.Id));
    }
    [SqlServerFact]
    public async Task Custom_percentages_are_saved_and_stale_updates_cannot_overwrite_them()
    {
        await using var context = database.CreateContext(); var repository = new BillingRepository(context);
        var service = new BillingSettings(repository);
        var created = (await service.SaveRateAsync(new(null, "Custom 12.5", 12.5m, false, true, null), CancellationToken.None))!;
        var updated = (await service.SaveRateAsync(new(created.Id, "Updated", 12.5m, false, false, created.Revision), CancellationToken.None))!;
        Assert.False(updated.Enabled);
        Assert.Null(await service.SaveRateAsync(new(created.Id, "Stale", 12.5m, false, true, created.Revision), CancellationToken.None));
        Assert.DoesNotContain(await repository.ListRatesAsync(true, CancellationToken.None), rate => rate.Id == created.Id);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveRateAsync(new(null, "Too many decimals", 12.555m, false, true, null), CancellationToken.None));
    }
}
