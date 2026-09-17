using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests;

public sealed class InfrastructureConnectionTerminatorContractTests
{
    public static IEnumerable<object[]> Connections => Enumerable.Range(1, 100)
        .Select(i => new object[] { $"Server=localhost;Database=MyShopTerm{i};Integrated Security=True;TrustServerCertificate=True" + new string(';', i % 5) });

    [Theory]
    [MemberData(nameof(Connections))]
    public void AddMyShopInfrastructure_AcceptsTrailingConnectionTerminators(string connectionString)
    {
        using var provider = new ServiceCollection().AddMyShopInfrastructure(connectionString).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
