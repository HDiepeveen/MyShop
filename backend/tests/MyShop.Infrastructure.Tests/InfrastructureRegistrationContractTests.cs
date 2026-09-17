using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests;

public sealed class InfrastructureRegistrationContractTests
{
    private const string ConnectionString = "Server=localhost;Database=MyShop;Integrated Security=True;TrustServerCertificate=True";
    private static readonly Type[] Services =
    [
        typeof(IProductRepository), typeof(IProductDeleter), typeof(IProductListRepository),
        typeof(IProductTypeRepository), typeof(IProductTypeListRepository), typeof(IProductTypeWriter),
        typeof(IProductTypeUsageRepository), typeof(IProductTypeDeleter), typeof(ICategoryRepository),
        typeof(ICategoryListRepository), typeof(ICategoryWriter), typeof(ICategoryHierarchyRepository),
        typeof(ICategoryUsageRepository), typeof(IProductSkuLookup)
    ];

    public static TheoryData<Type> ServiceData => new(Services);

    [Theory, MemberData(nameof(ServiceData))]
    public void Registration_HasSingleDescriptor(Type serviceType) => Assert.Single(Descriptors(serviceType));

    [Theory, MemberData(nameof(ServiceData))]
    public void Registration_IsScoped(Type serviceType) => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(Descriptors(serviceType)).Lifetime);

    [Theory, MemberData(nameof(ServiceData))]
    public void Registration_UsesFactory(Type serviceType) => Assert.NotNull(Assert.Single(Descriptors(serviceType)).ImplementationFactory);

    [Theory, MemberData(nameof(ServiceData))]
    public void Registration_HasNoInstanceImplementation(Type serviceType) => Assert.Null(Assert.Single(Descriptors(serviceType)).ImplementationInstance);

    [Theory, MemberData(nameof(ServiceData))]
    public void Registration_HasNoTypeImplementation(Type serviceType) => Assert.Null(Assert.Single(Descriptors(serviceType)).ImplementationType);

    [Theory, MemberData(nameof(ServiceData))]
    public void Registration_ResolvesInScope(Type serviceType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService(serviceType));
    }

    [Fact] public void Registration_HasExpectedServiceCount() => Assert.True(BuildServices().Length >= 15);
    [Fact] public void Registration_ContainsDbContext() => Assert.Single(BuildServices(), d => d.ServiceType == typeof(MyShopDbContext));
    [Fact] public void Registration_HasUniqueServiceTypes() { var ds = BuildServices(); Assert.Equal(ds.Length, ds.Select(d => d.ServiceType).Distinct().Count()); }
    [Fact] public void Registration_HasScopedApplicationLifetimes() => Assert.All(Services.Append(typeof(MyShopDbContext)), s => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(Descriptors(s)).Lifetime));
    [Fact] public void Registration_HasFactoriesForRepositories() => Assert.All(Services, s => Assert.NotNull(Assert.Single(Descriptors(s)).ImplementationFactory));
    [Fact] public void Registration_HasNoNullDescriptors() => Assert.All(BuildServices(), Assert.NotNull);
    [Fact] public void Registration_UsesSqlServerOptions() { using var p = BuildProvider(); using var s = p.CreateScope(); Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", s.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.ProviderName); }
    [Fact] public void Registration_UsesScopedContext() { using var p = BuildProvider(); using var s = p.CreateScope(); Assert.Same(s.ServiceProvider.GetRequiredService<MyShopDbContext>(), s.ServiceProvider.GetRequiredService<MyShopDbContext>()); }
    [Fact] public void Registration_UsesDifferentContextPerScope() { using var p = BuildProvider(); using var a = p.CreateScope(); using var b = p.CreateScope(); Assert.NotSame(a.ServiceProvider.GetRequiredService<MyShopDbContext>(), b.ServiceProvider.GetRequiredService<MyShopDbContext>()); }
    [Fact] public void Registration_ResolvesAllServices() { using var p = BuildProvider(); using var s = p.CreateScope(); Assert.All(Services, t => Assert.NotNull(s.ServiceProvider.GetRequiredService(t))); }
    [Fact] public void Registration_UsesExpectedConnection() { using var p = BuildProvider(); using var s = p.CreateScope(); var cs = new SqlConnectionStringBuilder(s.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.GetDbConnection().ConnectionString); Assert.Equal("MyShop", cs.InitialCatalog); }
    [Fact] public void Registration_DoesNotOpenConnection() { using var p = BuildProvider(); using var s = p.CreateScope(); Assert.Equal(System.Data.ConnectionState.Closed, s.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.GetDbConnection().State); }
    [Fact] public void Registration_IsRepeatable() { var a = BuildServices(); var b = BuildServices(); Assert.Equal(a.Length, b.Length); }
    [Fact] public void Registration_ReturnsSameCollection() { var services = new ServiceCollection(); Assert.Same(services, services.AddMyShopInfrastructure(ConnectionString)); }
    [Fact] public void Registration_RejectsNullCollection() => Assert.Throws<ArgumentNullException>(() => InfrastructureServiceCollectionExtensions.AddMyShopInfrastructure(null!, ConnectionString));
    [Fact] public void Registration_RejectsBlankConnection() => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddMyShopInfrastructure(" "));

    private static ServiceDescriptor[] Descriptors(Type serviceType) => BuildServices().Where(d => d.ServiceType == serviceType).ToArray();
    private static ServiceDescriptor[] BuildServices() { var services = new ServiceCollection(); services.AddMyShopInfrastructure(ConnectionString); return services.ToArray(); }
    private static ServiceProvider BuildProvider() => new ServiceCollection().AddMyShopInfrastructure(ConnectionString).BuildServiceProvider();
}
