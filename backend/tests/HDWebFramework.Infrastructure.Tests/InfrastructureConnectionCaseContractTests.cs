using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Infrastructure.Persistence;

namespace HDWebFramework.Infrastructure.Tests;

public sealed class InfrastructureConnectionCaseContractTests
{
    public static IEnumerable<object[]> Connections => Enumerable.Range(1, 100)
        .Select(i => new object[] { $"sErVeR=localhost;dAtAbAsE=HDWebFrameworkCase{i};iNtEgRaTeD sEcUrItY=True;TrUsT sErVeR cErTiFiCaTe=True" });

    [Theory]
    [MemberData(nameof(Connections))]
    public void AddHDWebFrameworkInfrastructure_AcceptsCaseInsensitiveConnectionKeywords(string connectionString)
    {
        using var provider = new ServiceCollection().AddHDWebFrameworkInfrastructure(connectionString).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>();
        var configured = new SqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.StartsWith("HDWebFrameworkCase", configured.InitialCatalog, StringComparison.Ordinal);
        Assert.True(configured.IntegratedSecurity);
        Assert.True(configured.TrustServerCertificate);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
