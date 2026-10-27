using System.Xml.Linq;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.CategoryNavigation;
using HDWebFramework.Application.Catalog.Seo;

namespace HDWebFramework.Api;

public static class SearchDiscoveryEndpoints
{
    public static void MapSearchDiscovery(this WebApplication app)
    {
        app.MapGet("/robots.txt", (HttpContext context) => Results.Text(
            "User-agent: *\nAllow: /\n\nSitemap: " + Origin(context) + "/sitemap.xml\n",
            "text/plain; charset=utf-8")).AllowAnonymous();
        app.MapGet("/sitemap.xml", async (HttpContext context, ICatalogSeoStore seo,
            IStorefrontCatalog catalog, ICategoryNavigationStore navigation, CancellationToken ct) =>
        {
            var settings = await seo.GetSettingsAsync(ct);
            var home = settings.Home.Normalize();
            var paths = new HashSet<string>(StringComparer.Ordinal) { "/assortiment" };
            if (home.Enabled)
            {
                paths.Add("/");
                foreach (var page in home.Items)
                    if (!string.IsNullOrWhiteSpace(page.WebAddress)) paths.Add("/" + page.WebAddress);
            }
            if (home.ContactEnabled) paths.Add("/contact");
            foreach (var category in await catalog.ListCategoriesAsync(ct))
            {
                var page = await navigation.ResolvePageAsync(category.Id.ToString("D"), ct);
                if (page is not null) paths.Add("/assortiment/" + page.WebAddress);
            }
            var at = DateTimeOffset.UtcNow;
            for (var offset = 0; ; offset += 500)
            {
                var page = await catalog.ListAsync(offset, 500, null, null, at, ct);
                foreach (var product in page.Items)
                    paths.Add("/winkel/" + (product.WebAddress ?? SeoText.AutomaticAddress(product.Name, product.Id)));
                if (page.Items.Count == 0 || offset + page.Items.Count >= page.TotalCount) break;
            }
            context.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Text(CreateSitemap(Origin(context), paths), "application/xml; charset=utf-8");
        }).AllowAnonymous();
    }

    private static string Origin(HttpContext context) =>
        context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;

    public static string CreateSitemap(string origin, IEnumerable<string> paths)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        return new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "urlset", paths.Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new XElement(ns + "url", new XElement(ns + "loc", origin.TrimEnd('/') + path)))))
            .ToString();
    }
}
