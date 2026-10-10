using System.Net;
using System.Text.RegularExpressions;
using MyShop.Application.Catalog.Seo;
using MyShop.Application.Catalog.Abstractions;
namespace MyShop.Api;
internal static class SeoHtml
{
    public static async Task<bool> WriteAsync(HttpContext context, string template, ICatalogSeoStore seo)
    {
        var path = context.Request.Path.Value ?? "/";
        var title = "MyShop"; string? description = null; string? canonical = null; var preview = ""; var index = false;
        if (path.TrimEnd('/') == "/winkel")
        {
            var settings = await seo.GetSettingsAsync(context.RequestAborted);
            title = settings.SeoTitle; canonical = "/winkel"; index = true;
            var catalog = context.RequestServices.GetRequiredService<IStorefrontCatalog>();
            var page = await catalog.ListAsync(0, 20, null, null, DateTimeOffset.UtcNow, context.RequestAborted);
            preview = "<main><h1>" + E(settings.Heading) + "</h1><ul>" + string.Concat(page.Items.Select(p =>
                "<li><a href=\"/winkel/" + E(p.WebAddress ?? p.Id.ToString("D")) + "\">" + E(p.Name) + "</a></li>")) + "</ul></main>";
        }
        else if (path.StartsWith("/winkel/", StringComparison.Ordinal) && path[8..].IndexOf('/') < 0)
        {
            var key = path[8..];
            if (!new[] { "betaling", "account", "registreren", "inloggen", "winkelmand", "e-mail-bevestigen", "wachtwoord-herstellen", "wachtwoord-vergeten" }.Contains(key))
            {
                var id = await seo.ResolveProductAsync(key, context.RequestAborted);
                var product = id is null ? null : await seo.GetProductAsync(id.Value, context.RequestAborted);
                if (product is null || !product.Published)
                { context.Response.StatusCode = 404; title = "Product niet beschikbaar · MyShop"; }
                else
                {
                    canonical = "/winkel/" + product.ResolvedAddress;
                    if (path != canonical)
                    { context.Response.Redirect(canonical + context.Request.QueryString, permanent: true); return true; }
                    title = product.ResolvedTitle; description = product.ResolvedDescription; index = true;
                    var catalog = context.RequestServices.GetRequiredService<IStorefrontCatalog>();
                    var detail = await catalog.GetAsync(MyShop.Domain.Catalog.ProductId.From(product.ProductId), context.RequestAborted);
                    preview = "<main><h1>" + E(product.Name) + "</h1><p>" + E(product.Description) + "</p><dl>" +
                        string.Concat((detail?.Attributes ?? []).Select(a => "<dt>" + E(a.Name) + "</dt><dd>" + E(a.Value) + "</dd>")) + "</dl></main>";
                }
            }
        }
        template = Regex.Replace(template, "<title>.*?</title>", _ => "<title>" + E(title) + "</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        template = Regex.Replace(template, "<meta\\s+[^>]*name=[\"'](?:description|robots)[\"'][^>]*>", "", RegexOptions.IgnoreCase);
        var tags = description is null ? "" : "<meta name=\"description\" content=\"" + E(description) + "\">";
        if (canonical is not null) tags += "<link rel=\"canonical\" href=\"" + E(context.Request.Scheme + "://" + context.Request.Host + canonical) + "\">";
        if (!index) { tags += "<meta name=\"robots\" content=\"noindex\">"; context.Response.Headers["X-Robots-Tag"] = "noindex"; }
        template = template.Replace("</head>", tags + "</head>", StringComparison.OrdinalIgnoreCase);
        if (preview.Length > 0) template = template.Replace("<app-root></app-root>", "<app-root>" + preview + "</app-root>", StringComparison.OrdinalIgnoreCase);
        await context.Response.WriteAsync(template, context.RequestAborted);
        return true;
    }
    private static string E(string value) => WebUtility.HtmlEncode(value);
}
