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
        var page = await repository.ListAsync(0, 100, CancellationToken.None);
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
        Assert.Null(await repository.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private static Order CreateOrder(DateTimeOffset placedAt, string customerName) => Order.Place(
        Guid.NewGuid(),
        placedAt,
        OrderCustomer.Create(customerName, "customer@example.test"),
        DeliveryAddress.Create("Teststraat 1", "1234 AB", "Utrecht", "NL"),
        [(Guid.NewGuid(), Guid.NewGuid(), "Shirt", "Blauw", 2, Money.Create(10m, "EUR"))]);
}
