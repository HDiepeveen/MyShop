using Microsoft.AspNetCore.DataProtection;

namespace HDWebFramework.Api;

public static class FrontendHosting
{
    public static void ConfigureHostingKeys(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        var path = configuration["Hosting:DataProtectionKeyPath"];
        var protection = services.AddDataProtection().SetApplicationName("HDWebFramework");
        if (!string.IsNullOrWhiteSpace(path))
            protection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(path, environment.ContentRootPath)));
    }

    public static void MapFrontend(this WebApplication app)
    {
        app.MapFallback(async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            var index = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
            if (!File.Exists(index))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            var seo = context.RequestServices.GetService<HDWebFramework.Application.Catalog.Seo.ICatalogSeoStore>();
            if (seo is null) await context.Response.SendFileAsync(index, context.RequestAborted);
            else await SeoHtml.WriteAsync(context, await File.ReadAllTextAsync(index, context.RequestAborted), seo);
        }).AllowAnonymous();
    }
}
