using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerMigrationTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task InitialMigrationCanBeAppliedRepeatedly()
    {
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        Assert.Single(await context.Database.GetAppliedMigrationsAsync());
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
            Assert.Single(await context.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await isolated.DisposeAsync();
        }
    }
}
