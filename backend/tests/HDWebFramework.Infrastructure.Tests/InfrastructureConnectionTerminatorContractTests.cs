using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Infrastructure.Persistence;

namespace HDWebFramework.Infrastructure.Tests;

public sealed class InfrastructureConnectionTerminatorContractTests
{
    public static IEnumerable<object[]> Connections => Enumerable.Range(1, 100)
        .Select(i => new object[] { $"Server=localhost;Database=HDWebFrameworkTerm{i};Integrated Security=True;TrustServerCertificate=True" + new string(';', i % 5) });

    [Theory]
    [MemberData(nameof(Connections))]
    public void AddHDWebFrameworkInfrastructure_AcceptsTrailingConnectionTerminators(string connectionString)
    {
        using var provider = new ServiceCollection().AddHDWebFrameworkInfrastructure(connectionString).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
