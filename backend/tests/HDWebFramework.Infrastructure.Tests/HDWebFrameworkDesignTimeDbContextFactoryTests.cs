using System.Data;
using Microsoft.EntityFrameworkCore;
using HDWebFramework.Infrastructure.Persistence;

namespace HDWebFramework.Infrastructure.Tests;

public sealed class HDWebFrameworkDesignTimeDbContextFactoryTests
{
    [Fact]
    public void CreatesSqlServerModelWithoutOpeningAConnection()
    {
        using var context = new HDWebFrameworkDesignTimeDbContextFactory().CreateDbContext([]);
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal("HDWebFrameworkDesignTime", context.Database.GetDbConnection().Database);
        Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        Assert.Equal(39, context.Model.GetEntityTypes().Count());
    }

    [Fact]
    public void EachCallCreatesAnIndependentContext()
    {
        var factory = new HDWebFrameworkDesignTimeDbContextFactory();
        using var first = factory.CreateDbContext([]);
        using var second = factory.CreateDbContext([]);
        Assert.NotSame(first, second);
        Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
    }
}
