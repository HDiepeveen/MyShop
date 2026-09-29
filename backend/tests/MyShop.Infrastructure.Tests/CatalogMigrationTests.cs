using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests;

public sealed class CatalogMigrationTests
{
    [Fact]
    public void MigrationSnapshotMatchesCurrentSqlServerModel()
    {
        using var context = new MyShopDesignTimeDbContextFactory().CreateDbContext([]);
        Assert.EndsWith("_ProductPresentation", context.Database.GetMigrations().Last());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void IdempotentScriptContainsEveryCatalogTableAndMigrationHistoryGuard()
    {
        using var context = new MyShopDesignTimeDbContextFactory().CreateDbContext([]);
        var script = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        foreach (var table in context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Distinct())
            Assert.Contains($"CREATE TABLE [{table}]", script);
        Assert.Contains("[__EFMigrationsHistory]", script);
        Assert.Contains("IF NOT EXISTS", script);
        foreach (var migration in context.Database.GetMigrations()) Assert.Contains(migration, script);
        Assert.Contains("BEGIN TRANSACTION", script);
    }

    [Fact]
    public void DownScriptRemovesCatalogTables()
    {
        using var context = new MyShopDesignTimeDbContextFactory().CreateDbContext([]);
        var migration = context.Database.GetMigrations().Last();
        var script = context.GetService<IMigrator>().GenerateScript(migration, Migration.InitialDatabase);
        foreach (var table in context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Distinct())
            Assert.Contains($"DROP TABLE [{table}]", script);
        Assert.Contains("DELETE FROM [__EFMigrationsHistory]", script);
    }
}
