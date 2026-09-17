using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Infrastructure.Tests;

public sealed class CatalogPersistenceRegistrationContractTests
{
    private const string ConnectionString =
        "Server=localhost;Database=MyShop;Integrated Security=True;TrustServerCertificate=True";

    private static readonly Type[] PersistenceAbstractions = typeof(IProductRepository).Assembly.GetTypes()
        .Where(type => type.IsInterface
            && type.Namespace == typeof(IProductRepository).Namespace)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> PersistenceAbstractionData => new(PersistenceAbstractions);

    [Theory]
    [MemberData(nameof(PersistenceAbstractionData))]
    public void CatalogPersistenceAbstraction_IsRegisteredExactlyOnce(Type serviceType)
    {
        var services = CreateServices();

        Assert.Single(services, descriptor => descriptor.ServiceType == serviceType);
    }

    [Theory]
    [MemberData(nameof(PersistenceAbstractionData))]
    public void CatalogPersistenceAbstraction_IsScoped(Type serviceType)
    {
        var descriptor = GetDescriptor(serviceType);

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Theory]
    [MemberData(nameof(PersistenceAbstractionData))]
    public void CatalogPersistenceAbstraction_UsesFactoryRegistration(Type serviceType)
    {
        var descriptor = GetDescriptor(serviceType);

        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Null(descriptor.ImplementationInstance);
        Assert.Null(descriptor.ImplementationType);
    }

    [Theory]
    [MemberData(nameof(PersistenceAbstractionData))]
    public void CatalogPersistenceAbstraction_CanBeResolvedWithoutOpeningDatabase(Type serviceType)
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(serviceType));
    }

    [Theory]
    [MemberData(nameof(PersistenceAbstractionData))]
    public void CatalogPersistenceAbstraction_ResolvesAssignableImplementation(Type serviceType)
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var implementation = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.True(serviceType.IsInstanceOfType(implementation));
    }

    [Theory]
    [MemberData(nameof(PersistenceAbstractionData))]
    public void CatalogPersistenceAbstraction_ReusesInstanceWithinScope(Type serviceType)
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var first = scope.ServiceProvider.GetRequiredService(serviceType);
        var second = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.Same(first, second);
    }

    [Theory]
    [MemberData(nameof(PersistenceAbstractionData))]
    public void CatalogPersistenceAbstraction_UsesDifferentInstanceAcrossScopes(Type serviceType)
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService(serviceType);
        var second = secondScope.ServiceProvider.GetRequiredService(serviceType);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void CatalogPersistenceRegistrationInventory_ContainsExpectedNumberOfAbstractions() =>
        Assert.Equal(14, PersistenceAbstractions.Length);

    [Fact]
    public void CatalogPersistenceRegistrationInventory_DoesNotExposeConcreteRepositories()
    {
        var concreteRegistrations = CreateServices().Where(descriptor =>
            descriptor.ServiceType.Namespace == "MyShop.Infrastructure.Persistence.Repositories");

        Assert.Empty(concreteRegistrations);
    }

    private static ServiceDescriptor GetDescriptor(Type serviceType) =>
        Assert.Single(CreateServices(), descriptor => descriptor.ServiceType == serviceType);

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddMyShopInfrastructure(ConnectionString);
        return services;
    }
}
