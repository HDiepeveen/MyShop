using System.Data;
using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests;

public sealed class MyShopDesignTimeDbContextFactoryTests
{
    [Fact]
    public void CreatesSqlServerModelWithoutOpeningAConnection()
    {
        using var context = new MyShopDesignTimeDbContextFactory().CreateDbContext([]);
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal("MyShopDesignTime", context.Database.GetDbConnection().Database);
        Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        Assert.Equal(34, context.Model.GetEntityTypes().Count());
    }

    [Fact]
    public void EachCallCreatesAnIndependentContext()
    {
        var factory = new MyShopDesignTimeDbContextFactory();
        using var first = factory.CreateDbContext([]);
        using var second = factory.CreateDbContext([]);
        Assert.NotSame(first, second);
        Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
    }
}
