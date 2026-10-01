using Microsoft.AspNetCore.Identity;
using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

public sealed class SqlServerDashboardReadTests
{
    [SqlServerFact]
    public async Task AggregatesOperationalDashboardSnapshot()
    {
        await using var database = new SqlServerDatabase();
        await database.InitializeAsync();
        var publishedType = ProductType.Create("Published type");
        var draftType = ProductType.Create("Draft type");
        var published = Product.Create("Published shirt", publishedType.Id, "Blue");
        published.SetPresentation(ProductPresentation.Create("Description",
            "https://example.com/shirt.jpg", "Shirt", true));
        var publishedVariant = published.Variants.Single();
        published.SetVariantSku(publishedVariant.Id, Sku.Create("SKU-DASHBOARD"));
        published.SetVariantStockQuantity(publishedVariant.Id, 3);
        var draft = Product.Create("Draft trousers", draftType.Id, "Black");
        draft.SetVariantStockQuantity(draft.Variants.Single().Id, 1);
        var paid = CreateOrder(DateTimeOffset.UtcNow.AddMinutes(-2), "Paid customer",
            Money.Create(12.50m, "EUR"));
        var shipped = CreateOrder(DateTimeOffset.UtcNow, "Shipped customer",
            Money.Create(7.50m, "EUR"));
        var refunded = CreateOrder(DateTimeOffset.UtcNow.AddMinutes(-1), "Refunded customer",
            Money.Create(99m, "EUR"));

        await using (var context = database.CreateContext())
        {
            await new ProductTypeRepository(context).AddAsync(publishedType, default);
            await new ProductTypeRepository(context).AddAsync(draftType, default);
            await new ProductRepository(context).AddAsync(published, default);
            await new ProductRepository(context).AddAsync(draft, default);
            context.Roles.Add(new IdentityRole { Id = "customer-role", Name = "Customer", NormalizedName = "CUSTOMER" });
            context.Users.Add(new IdentityUser { Id = "customer-user", UserName = "customer@example.test" });
            context.UserRoles.Add(new IdentityUserRole<string> { UserId = "customer-user", RoleId = "customer-role" });
            var orders = new OrderRepository(context);
            await orders.AddAsync(paid, Guid.NewGuid(), null, [], default);
            await orders.AddAsync(shipped, Guid.NewGuid(), null, [], default);
            await orders.AddAsync(refunded, Guid.NewGuid(), null, [], default);
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var repository = new OrderRepository(context);
            var paidDetail = (await repository.GetAsync(paid.Id, default))!;
            await repository.MarkPaidAsync(paid.Id, paidDetail.Revision, DateTimeOffset.UtcNow, "paid", default);
            var shippedDetail = (await repository.GetAsync(shipped.Id, default))!;
            var shippedPaid = await repository.MarkPaidAsync(shipped.Id, shippedDetail.Revision,
                DateTimeOffset.UtcNow, "paid", default);
            await repository.MarkShippedAsync(shipped.Id, shippedPaid!.Value, DateTimeOffset.UtcNow,
                "PostNL", "TRACK", default);
            var refundDetail = (await repository.GetAsync(refunded.Id, default))!;
            var refundPaid = await repository.MarkPaidAsync(refunded.Id, refundDetail.Revision,
                DateTimeOffset.UtcNow, "paid", default);
            await repository.RefundAsync(refunded.Id, refundPaid!.Value, DateTimeOffset.UtcNow,
                "refund", "Customer return", default);
        }

        await using var verification = database.CreateContext();
        var snapshot = await new DashboardReadRepository(verification).GetAsync(5, 2, default);

        Assert.Equal(2, snapshot.ProductCount);
        Assert.Equal(1, snapshot.PublishedProductCount);
        Assert.Equal(1, snapshot.CustomerCount);
        Assert.Equal(1, snapshot.Orders.Single(item => item.Status == OrderStatus.Paid).Count);
        Assert.Equal(1, snapshot.Orders.Single(item => item.Status == OrderStatus.Shipped).Count);
        Assert.Equal(1, snapshot.Orders.Single(item => item.Status == OrderStatus.Refunded).Count);
        var revenue = Assert.Single(snapshot.ActiveRevenue);
        Assert.Equal("EUR", revenue.Currency);
        Assert.Equal(20.00m, revenue.Amount);
        var lowStock = Assert.Single(snapshot.LowStock);
        Assert.Equal(published.Id.Value, lowStock.ProductId);
        Assert.Equal(publishedVariant.Id.Value, lowStock.VariantId);
        Assert.Equal("SKU-DASHBOARD", lowStock.Sku);
        Assert.Equal(3, lowStock.Quantity);
        Assert.Equal([shipped.Id, refunded.Id], snapshot.RecentOrders.Select(order => order.Id));
    }

    private static Order CreateOrder(DateTimeOffset placedAt, string customerName, Money price) =>
        Order.Place(Guid.NewGuid(), placedAt, OrderCustomer.Create(customerName, "customer@example.test"),
            DeliveryAddress.Create("Teststraat 1", "1234 AB", "Utrecht", "NL"),
            [(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 1, price)]);
}
