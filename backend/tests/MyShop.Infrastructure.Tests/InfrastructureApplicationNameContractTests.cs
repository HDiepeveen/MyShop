using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests;

public sealed class InfrastructureApplicationNameContractTests
{
    public static IEnumerable<object[]> Connections => Enumerable.Range(1, 100)
        .Select(i => new object[] { $"Server=localhost;Database=MyShopApp{i};Integrated Security=True;Application Name=MyShop.Contract.{i};TrustServerCertificate=True" });

    [Theory]
    [MemberData(nameof(Connections))]
    public void AddMyShopInfrastructure_PreservesApplicationName(string connectionString)
    {
        using var provider = new ServiceCollection().AddMyShopInfrastructure(connectionString).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        var configured = new SqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);

        Assert.StartsWith("MyShop.Contract.", configured.ApplicationName, StringComparison.Ordinal);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
