using MyShop.Application;
using MyShop.Infrastructure;

namespace MyShop.Api;

public static class MyShopServiceCollectionExtensions
{
    private const string ConnectionStringName = "MyShop";

    public static IServiceCollection AddMyShop(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddMyShopApplication();
        services.AddMyShopInfrastructure(connectionString);

        return services;
    }
}
