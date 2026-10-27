using System.Net;
using System.Xml.Linq;
using HDWebFramework.Api;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.CategoryNavigation;
using HDWebFramework.Application.Catalog.Seo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HDWebFramework.Api.Tests.Hosting;

public sealed class SearchDiscoveryTests
{
    [Fact]
    public async Task DiscoveryEndpointsExplicitlyAllowAnonymousAccessUnderAdminFallback()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        builder.Services.AddScoped<ICatalogSeoStore>(_ => null!);
        builder.Services.AddScoped<IStorefrontCatalog>(_ => null!);
        builder.Services.AddScoped<ICategoryNavigationStore>(_ => null!);
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSearchDiscovery();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var robots = await client.GetAsync("/robots.txt");
        Assert.Equal(HttpStatusCode.OK, robots.StatusCode);
        Assert.Equal("text/plain", robots.Content.Headers.ContentType!.MediaType);
        Assert.Contains("Sitemap: " + client.BaseAddress + "sitemap.xml", await robots.Content.ReadAsStringAsync());
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        Assert.Equal(2, endpoints.Length);
        Assert.All(endpoints, endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>()));
        await app.StopAsync();
    }

    [Fact]
    public void SitemapUsesAbsoluteEscapedUniqueUrlsAndTheSitemapNamespace()
    {
        var xml = XDocument.Parse(SearchDiscoveryEndpoints.CreateSitemap("https://example.nl/", ["/", "/apk", "/apk", "/winkel/test?a=1&b=2"]));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        Assert.Equal(ns + "urlset", xml.Root!.Name);
        Assert.Equal(new[] { "https://example.nl/", "https://example.nl/apk", "https://example.nl/winkel/test?a=1&b=2" }, xml.Descendants(ns + "loc").Select(loc => loc.Value));
    }
}
