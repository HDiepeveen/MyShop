using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerMigrationTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task Type_headings_migration_transfers_existing_texts_and_refuses_conflicting_texts()
    {
        foreach (var conflict in new[] { false, true })
        {
            var isolated = new SqlServerDatabase();
            await isolated.InitializeAsync();
            try
            {
                await using var context = isolated.CreateContext();
                await context.GetService<IMigrator>().MigrateAsync("20261010125930_ProductSectionHeadings");
                var typeId = Guid.NewGuid();
                await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductTypes (Id, Name) VALUES ({typeId}, {"Cars"})");
                foreach (var text in new[] { "Over deze auto", conflict ? "Different text" : "Over deze auto" })
                {
                    var productId = Guid.NewGuid(); var revision = Guid.NewGuid();
                    await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Products (Id, ProductTypeId, Name, Version) VALUES ({productId}, {typeId}, {"Car"}, {revision})");
                    await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductSeos (ProductId, Version, AboutHeading, AttributesHeading) VALUES ({productId}, {revision}, {text}, {"Voertuiggegevens"})");
                }
                if (conflict)
                {
                    var error = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => context.Database.MigrateAsync());
                    Assert.Equal(51000, error.Number);
                    Assert.Equal(30, (await context.Database.GetAppliedMigrationsAsync()).Count());
                    Assert.Equal(2, await context.Database.SqlQueryRaw<string>("SELECT DISTINCT AboutHeading AS Value FROM ProductSeos").CountAsync());
                }
                else
                {
                    await context.Database.MigrateAsync();
                    var type = await context.ProductTypes.AsNoTracking().SingleAsync(x => x.Id == typeId);
                    Assert.Equal("Over deze auto", type.AboutHeading); Assert.Equal("Voertuiggegevens", type.AttributesHeading);
                    Assert.NotEqual(Guid.Empty, type.SectionHeadingsRevision);
                    Assert.Equal(2, await context.ProductSeos.CountAsync());
                }
            }
            finally { await isolated.DisposeAsync(); }
        }
    }

    [SqlServerFact]
    public async Task Branding_migration_keeps_existing_heading_title_and_revision()
    {
        var isolated = new SqlServerDatabase();
        await isolated.InitializeAsync();
        try
        {
            await using var context = isolated.CreateContext();
            await context.GetService<IMigrator>().MigrateAsync("20261010115938_CatalogSeoNaming");
            var revision = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [CatalogSeoSettings] SET [Heading] = {"Existing heading"}, [SeoTitle] = {"Existing title"}, [Version] = {revision}");
            await context.Database.MigrateAsync();
            var settings = await context.CatalogSeoSettings.AsNoTracking().SingleAsync();
            Assert.Equal("Existing heading", settings.Heading);
            Assert.Equal("Existing title", settings.SeoTitle);
            Assert.Equal(revision, settings.Version);
            Assert.Equal("MyShop", settings.ShopName);
            Assert.Equal("Welkom bij MyShop", settings.WelcomeText);
            Assert.Equal("Bekijk onze producten en kies de variant die bij je past.", settings.Introduction);
        }
        finally { await isolated.DisposeAsync(); }
    }

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
        Assert.Equal(32, (await context.Database.GetAppliedMigrationsAsync()).Count());
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
            Assert.Equal(32, (await context.Database.GetAppliedMigrationsAsync()).Count());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await isolated.DisposeAsync();
        }
    }
}
