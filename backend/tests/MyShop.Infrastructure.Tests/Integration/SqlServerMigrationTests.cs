using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerMigrationTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task LaterMigrationsPreserveExistingDataAndSeedPaymentOptions()
    {
        var isolated = new SqlServerDatabase();
        await isolated.InitializeAsync();
        try
        {
            await using var context = isolated.CreateContext();
            await context.GetService<IMigrator>().MigrateAsync(context.Database.GetMigrations().First());
            var id = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [ProductTypes] ([Id], [Name]) VALUES ({id}, {"Existing catalog type"})");
            var productId = Guid.NewGuid();
            var version = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Products] ([Id], [ProductTypeId], [Name], [Version]) VALUES ({productId}, {id}, {"Existing product"}, {version})");
            var variantId = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [ProductVariants] ([Id], [ProductId], [Name], [Ordinal]) VALUES ({variantId}, {productId}, {"Existing variant"}, {0})");
            await context.GetService<IMigrator>().MigrateAsync("20260930085252_PlaceOrders");
            var orderId = Guid.NewGuid();
            var checkoutToken = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [Orders] ([Id], [CheckoutToken], [Number], [PlacedAt], [CustomerName], [Email],
                    [AddressLine], [PostalCode], [City], [CountryCode], [PaymentMethod], [Status])
                VALUES ({orderId}, {checkoutToken}, {"MS-EXISTING"}, {DateTimeOffset.UtcNow}, {"Existing customer"},
                    {"customer@example.test"}, {"Street 1"}, {"1234 AB"}, {"Utrecht"}, {"NL"}, {1}, {1})
                """);
            await context.Database.MigrateAsync();
            var product = await context.Products.SingleAsync(p => p.Id == productId);
            Assert.False(product.IsPublished);
            Assert.Equal("", product.Description);
            Assert.Null(product.ImageUrl);
            Assert.Equal(version, product.Version);
            Assert.Null((await context.ProductVariants.SingleAsync(item => item.Id == variantId)).StockQuantity);
            Assert.Equal("Existing catalog type", (await context.ProductTypes.SingleAsync(p => p.Id == id)).Name);
            Assert.Empty(await context.Users.ToListAsync());
            var paymentOptions = await context.PaymentOptions.SingleAsync();
            Assert.True(paymentOptions.PayLaterEnabled);
            Assert.False(paymentOptions.OnlinePaymentEnabled);
            Assert.Null(paymentOptions.PayLaterInstructions);
            Assert.NotEqual(Guid.Empty, paymentOptions.Version);
            var order = await context.Orders.SingleAsync(item => item.Id == orderId);
            Assert.NotEqual(Guid.Empty, order.Version);
            Assert.Null(order.PaidAt);
            Assert.Null(order.PaymentInstructions);
            Assert.Null(order.PaymentReference);
            Assert.Null(order.ShippedAt);
            Assert.Null(order.ShippingCarrier);
            Assert.Null(order.TrackingCode);
            Assert.Null(order.CancelledAt);
            Assert.Null(order.CancellationReason);
            Assert.Null(order.RefundedAt);
            Assert.Null(order.RefundReference);
            Assert.Null(order.RefundReason);
            Assert.Null(order.CustomerUserId);
            Assert.Null(order.DeliveryMethodId);
            var delivery = await context.DeliveryMethods.SingleAsync();
            Assert.Equal("Standaardbezorging", delivery.Name);
            Assert.True(delivery.Enabled);
        }
        finally { await isolated.DisposeAsync(); }
    }

    [SqlServerFact]
    public async Task InitialMigrationCanBeAppliedRepeatedly()
    {
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        Assert.Equal(22, (await context.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.True(await context.Database.CanConnectAsync());
    }

    [SqlServerFact]
    public async Task InitialMigrationCanBeRolledBackAndAppliedAgainOnAnIsolatedDatabase()
    {
        var isolated = new SqlServerDatabase();
        await isolated.InitializeAsync();
        try
        {
            await using var context = isolated.CreateContext();
            await context.GetService<IMigrator>().MigrateAsync(Migration.InitialDatabase);
            Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
            await context.Database.MigrateAsync();
            Assert.Equal(22, (await context.Database.GetAppliedMigrationsAsync()).Count());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await isolated.DisposeAsync();
        }
    }
}
