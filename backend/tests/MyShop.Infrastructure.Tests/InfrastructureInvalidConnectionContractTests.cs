using Microsoft.Extensions.DependencyInjection;

namespace MyShop.Infrastructure.Tests;

public sealed class InfrastructureInvalidConnectionContractTests
{
    public static IEnumerable<object[]> InvalidConnections => Enumerable.Range(1, 100)
        .Select(index => new object[] { new string(' ', index) });

    [Theory]
    [MemberData(nameof(InvalidConnections))]
    public void AddMyShopInfrastructure_RejectsWhitespaceOnlyConnectionStrings(string connectionString)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddMyShopInfrastructure(connectionString));
        Assert.Empty(services);
    }
}
