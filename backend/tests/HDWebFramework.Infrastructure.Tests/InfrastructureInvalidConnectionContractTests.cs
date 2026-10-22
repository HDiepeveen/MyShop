using Microsoft.Extensions.DependencyInjection;

namespace HDWebFramework.Infrastructure.Tests;

public sealed class InfrastructureInvalidConnectionContractTests
{
    public static IEnumerable<object[]> InvalidConnections => Enumerable.Range(1, 100)
        .Select(index => new object[] { new string(' ', index) });

    [Theory]
    [MemberData(nameof(InvalidConnections))]
    public void AddHDWebFrameworkInfrastructure_RejectsWhitespaceOnlyConnectionStrings(string connectionString)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddHDWebFrameworkInfrastructure(connectionString));
        Assert.Empty(services);
    }
}
