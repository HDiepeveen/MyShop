using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Application.Catalog.CreateProduct;

namespace HDWebFramework.Application.Tests;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    public static TheoryData<Type> UseCaseTypes
    {
        get
        {
            var useCases = typeof(CreateProduct).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                    && type.Namespace?.StartsWith("HDWebFramework.Application.Catalog.", StringComparison.Ordinal) == true
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
    public void AddHDWebFrameworkApplication_RejectsNullServices()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationServiceCollectionExtensions.AddHDWebFrameworkApplication(null!));
    }

    [Fact]
    public void AddHDWebFrameworkApplication_ReturnsSameCollection()
    {
        var services = new ServiceCollection();

        var returned = services.AddHDWebFrameworkApplication();

        Assert.Same(services, returned);
    }

    [Theory]
    [MemberData(nameof(UseCaseTypes))]
    public void AddHDWebFrameworkApplication_RegistersEachUseCaseAsScoped(Type useCaseType)
    {
        var services = new ServiceCollection();

        services.AddHDWebFrameworkApplication();

        var descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == useCaseType);
        Assert.Equal(useCaseType, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
