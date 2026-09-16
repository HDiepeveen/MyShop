using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Catalog.CreateProduct;

namespace MyShop.Application.Tests;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    public static TheoryData<Type> UseCaseTypes
    {
        get
        {
            var useCases = typeof(CreateProduct).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                    && type.Namespace?.StartsWith("MyShop.Application.Catalog.", StringComparison.Ordinal) == true
                    && type.GetMethods().Any(method =>
                        method.Name == "ExecuteAsync" && method.DeclaringType == type))
                .OrderBy(type => type.FullName);

            var data = new TheoryData<Type>();
            foreach (var useCase in useCases)
                data.Add(useCase);

            return data;
        }
    }

    [Fact]
    public void AddMyShopApplication_RejectsNullServices()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationServiceCollectionExtensions.AddMyShopApplication(null!));
    }

    [Fact]
    public void AddMyShopApplication_ReturnsSameCollection()
    {
        var services = new ServiceCollection();

        var returned = services.AddMyShopApplication();

        Assert.Same(services, returned);
    }

    [Theory]
    [MemberData(nameof(UseCaseTypes))]
    public void AddMyShopApplication_RegistersEachUseCaseAsScoped(Type useCaseType)
    {
        var services = new ServiceCollection();

        services.AddMyShopApplication();

        var descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == useCaseType);
        Assert.Equal(useCaseType, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
