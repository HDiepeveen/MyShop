using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Infrastructure.Persistence;

namespace HDWebFramework.Infrastructure.Tests;

public sealed class InfrastructureConnectionStringContractTests
{
    public static IEnumerable<object[]> ConnectionStrings => Enumerable.Range(1, 100)
        .Select(index => new object[] { $"Server=localhost;Database=HDWebFrameworkContract{index};Integrated Security=True;TrustServerCertificate=True" });

    [Theory]
    [MemberData(nameof(ConnectionStrings))]
    public void AddHDWebFrameworkInfrastructure_PreservesDatabaseConfiguration(string connectionString)
    {
        var services = new ServiceCollection().AddHDWebFrameworkInfrastructure(connectionString);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        var configured = new SqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);
        Assert.StartsWith("HDWebFrameworkContract", configured.InitialCatalog, StringComparison.Ordinal);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
