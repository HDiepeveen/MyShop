using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests;

public sealed class InfrastructureServiceCollectionExtensionsTests
{
    private const string ConnectionString =
        "Server=localhost;Database=MyShop;Integrated Security=True;TrustServerCertificate=True";

    [Fact]
    public void AddMyShopInfrastructure_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            InfrastructureServiceCollectionExtensions.AddMyShopInfrastructure(null!, ConnectionString));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddMyShopInfrastructure(null!));
        Assert.Throws<ArgumentException>(() => services.AddMyShopInfrastructure(""));
        Assert.Throws<ArgumentException>(() => services.AddMyShopInfrastructure(" "));
        Assert.Empty(services);
    }

    [Fact]
    public void AddMyShopInfrastructure_ReturnsSameCollectionAndRegistersScopedServices()
    {
        var services = new ServiceCollection();

        var returned = services.AddMyShopInfrastructure(ConnectionString);

        Assert.Same(services, returned);
        AssertScoped<MyShopDbContext>(services);
        AssertScopedFactory<IProductRepository>(services);
        AssertScopedFactory<IProductListRepository>(services);
        AssertScopedFactory<IProductTypeRepository>(services);
        AssertScopedFactory<IProductTypeListRepository>(services);
        AssertScopedFactory<IProductTypeWriter>(services);
        AssertScopedFactory<ICategoryRepository>(services);
        AssertScopedFactory<ICategoryListRepository>(services);
        AssertScopedFactory<ICategoryWriter>(services);
        AssertScopedFactory<ICategoryHierarchyRepository>(services);
        AssertScopedFactory<IProductSkuLookup>(services);
    }

    [Fact]
    public void AddMyShopInfrastructure_ConfiguresSqlServerWithoutOpeningConnection()
    {
        var services = new ServiceCollection();
        services.AddMyShopInfrastructure(ConnectionString);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        var configured = new SqlConnectionStringBuilder(
            context.Database.GetDbConnection().ConnectionString);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal("localhost", configured.DataSource);
        Assert.Equal("MyShop", configured.InitialCatalog);
        Assert.True(configured.IntegratedSecurity);
        Assert.True(configured.TrustServerCertificate);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public void AddMyShopInfrastructure_UsesOneInstancePerScope()
    {
        var services = new ServiceCollection();
        services.AddMyShopInfrastructure(ConnectionString);

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstRepository = firstScope.ServiceProvider.GetRequiredService<IProductRepository>();
        var repeatedRepository = firstScope.ServiceProvider.GetRequiredService<IProductRepository>();
        var secondRepository = secondScope.ServiceProvider.GetRequiredService<IProductRepository>();

        Assert.Same(firstRepository, repeatedRepository);
        Assert.NotSame(firstRepository, secondRepository);
    }

    private static void AssertScoped<TService>(IServiceCollection services)
    {
        var descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == typeof(TService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    private static void AssertScopedFactory<TService>(IServiceCollection services)
    {
        var descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == typeof(TService));
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
