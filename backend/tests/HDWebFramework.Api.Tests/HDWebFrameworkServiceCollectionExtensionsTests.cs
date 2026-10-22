using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Api;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.CreateProduct;
using HDWebFramework.Infrastructure.Persistence;

namespace HDWebFramework.Api.Tests;

public sealed class HDWebFrameworkServiceCollectionExtensionsTests
{
    private const string ConnectionString =
        "Server=localhost;Database=HDWebFramework;Integrated Security=True;TrustServerCertificate=True";

    [Fact]
    public void AddHDWebFramework_RejectsInvalidArguments()
    {
        var configuration = CreateConfiguration(ConnectionString);

        Assert.Throws<ArgumentNullException>(() =>
            HDWebFrameworkServiceCollectionExtensions.AddHDWebFramework(null!, configuration));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddHDWebFramework(null!));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddHDWebFramework_RejectsMissingConnectionString(string? connectionString)
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(connectionString);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddHDWebFramework(configuration));

        Assert.Equal("Connection string 'HDWebFramework' is not configured.", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddHDWebFramework_ComposesApplicationAndInfrastructure()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(ConnectionString);

        var returned = services.AddHDWebFramework(configuration);

        Assert.Same(services, returned);
        AssertScoped<CreateProduct>(services);
        AssertScoped<IProductRepository>(services);
        AssertScoped<HDWebFrameworkDbContext>(services);
    }

    [Fact]
    public void AddHDWebFramework_ConfiguresSqlServerWithoutOpeningConnection()
    {
        var services = new ServiceCollection();
        services.AddHDWebFramework(CreateConfiguration(ConnectionString));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rename_respects_legacy_production_override_and_new_environment_override(bool overrideNew)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:HDWebFramework"] = ConnectionString })
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:MyShop"] = ConnectionString.Replace("Database=HDWebFramework", "Database=Production") })
            .AddInMemoryCollection(overrideNew ? new Dictionary<string, string?> { ["ConnectionStrings:HDWebFramework"] = ConnectionString.Replace("Database=HDWebFramework", "Database=Override") } : [])
            .Build();
        var services = new ServiceCollection();
        services.AddHDWebFramework(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Equal(overrideNew ? "Override" : "Production", scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>().Database.GetDbConnection().Database);
    }

    private static IConfiguration CreateConfiguration(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HDWebFramework"] = connectionString
            })
            .Build();

    private static void AssertScoped<TService>(IServiceCollection services)
    {
        var descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == typeof(TService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
