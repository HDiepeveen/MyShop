using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.CreateProduct;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests;

public sealed class MyShopServiceCollectionExtensionsTests
{
    private const string ConnectionString =
        "Server=localhost;Database=MyShop;Integrated Security=True;TrustServerCertificate=True";

    [Fact]
    public void AddMyShop_RejectsInvalidArguments()
    {
        var configuration = CreateConfiguration(ConnectionString);

        Assert.Throws<ArgumentNullException>(() =>
            MyShopServiceCollectionExtensions.AddMyShop(null!, configuration));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddMyShop(null!));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddMyShop_RejectsMissingConnectionString(string? connectionString)
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(connectionString);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMyShop(configuration));

        Assert.Equal("Connection string 'MyShop' is not configured.", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddMyShop_ComposesApplicationAndInfrastructure()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(ConnectionString);

        var returned = services.AddMyShop(configuration);

        Assert.Same(services, returned);
        AssertScoped<CreateProduct>(services);
        AssertScoped<IProductRepository>(services);
        AssertScoped<MyShopDbContext>(services);
    }

    [Fact]
    public void AddMyShop_ConfiguresSqlServerWithoutOpeningConnection()
    {
        var services = new ServiceCollection();
        services.AddMyShop(CreateConfiguration(ConnectionString));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    private static IConfiguration CreateConfiguration(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MyShop"] = connectionString
            })
            .Build();

    private static void AssertScoped<TService>(IServiceCollection services)
    {
        var descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == typeof(TService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
