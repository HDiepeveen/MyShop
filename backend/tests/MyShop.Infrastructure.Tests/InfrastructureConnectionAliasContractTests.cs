using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests;

public sealed class InfrastructureConnectionAliasContractTests
{
    public static IEnumerable<object[]> Connections => Enumerable.Range(1, 100)
        .Select(i => new object[] { $"Data Source=localhost;Initial Catalog=MyShopAlias{i};Integrated Security=True;TrustServerCertificate=True" });

    [Theory]
    [MemberData(nameof(Connections))]
    public void AddMyShopInfrastructure_AcceptsSqlServerAliasConnectionStrings(string connectionString)
    {
        using var provider = new ServiceCollection().AddMyShopInfrastructure(connectionString).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        var configured = new SqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.StartsWith("MyShopAlias", configured.InitialCatalog, StringComparison.Ordinal);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
