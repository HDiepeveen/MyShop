using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Infrastructure.Persistence;
using HDWebFramework.Infrastructure.Persistence.Repositories;

namespace HDWebFramework.Infrastructure.Tests;

public sealed class InfrastructureServiceCollectionExtensionsTests
{
    private const string ConnectionString =
        "Server=localhost;Database=HDWebFramework;Integrated Security=True;TrustServerCertificate=True";

    [Fact]
    public void AddHDWebFrameworkInfrastructure_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            InfrastructureServiceCollectionExtensions.AddHDWebFrameworkInfrastructure(null!, ConnectionString));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddHDWebFrameworkInfrastructure(null!));
        Assert.Throws<ArgumentException>(() => services.AddHDWebFrameworkInfrastructure(""));
        Assert.Throws<ArgumentException>(() => services.AddHDWebFrameworkInfrastructure(" "));
        Assert.Empty(services);
    }

    [Fact]
    public void AddHDWebFrameworkInfrastructure_ReturnsSameCollectionAndRegistersScopedServices()
    {
        var services = new ServiceCollection();

        var returned = services.AddHDWebFrameworkInfrastructure(ConnectionString);

        Assert.Same(services, returned);
        AssertScoped<HDWebFrameworkDbContext>(services);
        AssertScopedFactory<IProductRepository>(services);
        AssertScopedFactory<IProductListRepository>(services);
        AssertScopedFactory<IProductTypeRepository>(services);
        AssertScopedFactory<IProductTypeListRepository>(services);
        AssertScopedFactory<IProductTypeWriter>(services);
        AssertScopedFactory<IProductTypeUsageRepository>(services);
        AssertScopedFactory<IProductTypeDeleter>(services);
        AssertScopedFactory<ICategoryRepository>(services);
        AssertScopedFactory<ICategoryListRepository>(services);
        AssertScopedFactory<ICategoryWriter>(services);
        AssertScopedFactory<ICategoryHierarchyRepository>(services);
        AssertScopedFactory<ICategoryUsageRepository>(services);
        AssertScopedFactory<IProductSkuLookup>(services);
    }

    [Fact]
    public void AddHDWebFrameworkInfrastructure_ConfiguresSqlServerWithoutOpeningConnection()
    {
        var services = new ServiceCollection();
        services.AddHDWebFrameworkInfrastructure(ConnectionString);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>();
        var configured = new SqlConnectionStringBuilder(
            context.Database.GetDbConnection().ConnectionString);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal("localhost", configured.DataSource);
        Assert.Equal("HDWebFramework", configured.InitialCatalog);
        Assert.True(configured.IntegratedSecurity);
        Assert.True(configured.TrustServerCertificate);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public void AddHDWebFrameworkInfrastructure_UsesOneInstancePerScope()
    {
        var services = new ServiceCollection();
        services.AddHDWebFrameworkInfrastructure(ConnectionString);

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
