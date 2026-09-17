using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests;

public sealed class InfrastructureConnectionStringContractTests
{
    public static IEnumerable<object[]> ConnectionStrings => Enumerable.Range(1, 100)
        .Select(index => new object[] { $"Server=localhost;Database=MyShopContract{index};Integrated Security=True;TrustServerCertificate=True" });

    [Theory]
    [MemberData(nameof(ConnectionStrings))]
    public void AddMyShopInfrastructure_PreservesDatabaseConfiguration(string connectionString)
    {
        var services = new ServiceCollection().AddMyShopInfrastructure(connectionString);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        var configured = new SqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);
        Assert.StartsWith("MyShopContract", configured.InitialCatalog, StringComparison.Ordinal);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
