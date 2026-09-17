using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests;

public sealed class InfrastructureConnectionCaseContractTests
{
    public static IEnumerable<object[]> Connections => Enumerable.Range(1, 100)
        .Select(i => new object[] { $"sErVeR=localhost;dAtAbAsE=MyShopCase{i};iNtEgRaTeD sEcUrItY=True;TrUsT sErVeR cErTiFiCaTe=True" });

    [Theory]
    [MemberData(nameof(Connections))]
    public void AddMyShopInfrastructure_AcceptsCaseInsensitiveConnectionKeywords(string connectionString)
    {
        using var provider = new ServiceCollection().AddMyShopInfrastructure(connectionString).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        var configured = new SqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.StartsWith("MyShopCase", configured.InitialCatalog, StringComparison.Ordinal);
        Assert.True(configured.IntegratedSecurity);
        Assert.True(configured.TrustServerCertificate);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
