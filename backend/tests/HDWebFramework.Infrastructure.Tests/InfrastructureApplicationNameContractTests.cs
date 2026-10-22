using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Infrastructure.Persistence;

namespace HDWebFramework.Infrastructure.Tests;

public sealed class InfrastructureApplicationNameContractTests
{
    public static IEnumerable<object[]> Connections => Enumerable.Range(1, 100)
        .Select(i => new object[] { $"Server=localhost;Database=HDWebFrameworkApp{i};Integrated Security=True;Application Name=HDWebFramework.Contract.{i};TrustServerCertificate=True" });

    [Theory]
    [MemberData(nameof(Connections))]
    public void AddHDWebFrameworkInfrastructure_PreservesApplicationName(string connectionString)
    {
        using var provider = new ServiceCollection().AddHDWebFrameworkInfrastructure(connectionString).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>();
        var configured = new SqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);

        Assert.StartsWith("HDWebFramework.Contract.", configured.ApplicationName, StringComparison.Ordinal);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
