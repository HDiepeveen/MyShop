using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Catalog.Abstractions;
using CreateProductUseCase = MyShop.Application.Catalog.CreateProduct.CreateProduct;

namespace MyShop.Infrastructure.Tests;

public sealed class CatalogUseCaseDependencyContractTests
{
    private const string ConnectionString =
        "Server=localhost;Database=MyShop;Integrated Security=True;TrustServerCertificate=True";

    private static readonly Type[] PersistenceAbstractions = typeof(IProductRepository).Assembly.GetTypes()
        .Where(type => type.IsInterface
            && type.Namespace == typeof(IProductRepository).Namespace)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    private static readonly Type[] UseCases = typeof(CreateProductUseCase).Assembly.GetTypes()
        .Where(IsCatalogUseCase)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> UseCaseData => new(UseCases);

    [Theory]
    [MemberData(nameof(UseCaseData))]
    public void CatalogUseCase_DeclaresPersistenceDependency(Type useCaseType)
    {
        var constructor = Assert.Single(useCaseType.GetConstructors());

        Assert.NotEmpty(constructor.GetParameters());
    }

    [Theory]
    [MemberData(nameof(UseCaseData))]
    public void CatalogUseCase_DependsOnlyOnCatalogPersistenceAbstractions(Type useCaseType)
    {
        var constructor = Assert.Single(useCaseType.GetConstructors());

        Assert.All(constructor.GetParameters(), parameter =>
            Assert.Contains(parameter.ParameterType, PersistenceAbstractions));
    }

    [Theory]
    [MemberData(nameof(UseCaseData))]
    public void CatalogUseCase_EachDependencyHasScopedInfrastructureRegistration(Type useCaseType)
    {
        var services = CreateServices();
        var constructor = Assert.Single(useCaseType.GetConstructors());

        Assert.All(constructor.GetParameters(), parameter =>
        {
            var descriptor = Assert.Single(services, candidate =>
                candidate.ServiceType == parameter.ParameterType);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        });
    }

    [Fact]
    public void CatalogUseCaseDependencyInventory_ContainsExpectedNumberOfUseCases() =>
        Assert.Equal(39, UseCases.Length);

    [Fact]
    public void CatalogUseCaseDependencyInventory_ConsumesEveryPersistenceAbstraction()
    {
        var consumedTypes = UseCases
            .SelectMany(type => Assert.Single(type.GetConstructors()).GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        Assert.Equal(PersistenceAbstractions, consumedTypes);
    }

    [Fact]
    public void CatalogUseCaseDependencyInventory_HasNoDuplicateConstructorDependencies()
    {
        Assert.All(UseCases, type =>
        {
            var parameterTypes = Assert.Single(type.GetConstructors())
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.Equal(parameterTypes.Length, parameterTypes.Distinct().Count());
        });
    }

    [Fact]
    public void CatalogUseCaseDependencyInventory_InfrastructureDoesNotRegisterUseCases()
    {
        var registeredTypes = CreateServices().Select(descriptor => descriptor.ServiceType);

        Assert.Empty(registeredTypes.Intersect(UseCases));
    }

    private static bool IsCatalogUseCase(Type type) =>
        type is { IsClass: true, IsAbstract: false, IsPublic: true }
        && type.Namespace?.StartsWith("MyShop.Application.Catalog.", StringComparison.Ordinal) == true
        && type.GetMethods().Any(method =>
            method.Name == "ExecuteAsync" && method.DeclaringType == type);

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddMyShopInfrastructure(ConnectionString);
        return services;
    }
}
