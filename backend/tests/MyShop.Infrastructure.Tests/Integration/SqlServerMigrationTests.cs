using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerMigrationTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task LaterMigrationsPreserveExistingCatalogDataAndSeedPaymentOptions()
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
            await context.Database.MigrateAsync();
            var product = await context.Products.SingleAsync(p => p.Id == productId);
            Assert.False(product.IsPublished);
            Assert.Equal("", product.Description);
            Assert.Null(product.ImageUrl);
            Assert.Equal(version, product.Version);
            Assert.Equal("Existing catalog type", (await context.ProductTypes.SingleAsync(p => p.Id == id)).Name);
            Assert.Empty(await context.Users.ToListAsync());
            var paymentOptions = await context.PaymentOptions.SingleAsync();
            Assert.True(paymentOptions.PayLaterEnabled);
            Assert.False(paymentOptions.OnlinePaymentEnabled);
            Assert.NotEqual(Guid.Empty, paymentOptions.Version);
        }
        finally { await isolated.DisposeAsync(); }
    }

    [SqlServerFact]
    public async Task InitialMigrationCanBeAppliedRepeatedly()
    {
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        Assert.Equal(4, (await context.Database.GetAppliedMigrationsAsync()).Count());
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
            Assert.Equal(4, (await context.Database.GetAppliedMigrationsAsync()).Count());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await isolated.DisposeAsync();
        }
    }
}
