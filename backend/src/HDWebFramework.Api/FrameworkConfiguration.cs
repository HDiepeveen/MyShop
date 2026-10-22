namespace HDWebFramework.Api;

internal static class FrameworkConfiguration
{
    internal static string? ConnectionString(IConfiguration configuration)
    {
        // Honor provider priority when an existing production file still uses the previous key.
        if (configuration is IConfigurationRoot root)
            foreach (var provider in root.Providers.Reverse())
            {
                if (provider.TryGet("ConnectionStrings:HDWebFramework", out var current)) return current;
                if (provider.TryGet("ConnectionStrings:MyShop", out var previous)) return previous;
            }
        return configuration.GetConnectionString("HDWebFramework");
    }
}
