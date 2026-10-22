using HDWebFramework.Application;
using HDWebFramework.Infrastructure;

namespace HDWebFramework.Api;

public static class HDWebFrameworkServiceCollectionExtensions
{
    private const string ConnectionStringName = "HDWebFramework";

    public static IServiceCollection AddHDWebFramework(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = FrameworkConfiguration.ConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddHDWebFrameworkApplication();
        services.AddHDWebFrameworkInfrastructure(connectionString, configuration);

        return services;
    }
}
