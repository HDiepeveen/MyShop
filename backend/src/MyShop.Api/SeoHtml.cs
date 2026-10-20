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
        if (path.TrimEnd('/') == "/winkel")
        {
            context.Response.Redirect("/assortiment" + context.Request.QueryString, permanent: true);
            return true;
        }
        if (path.TrimEnd('/') == "/winkel/informatie/bedrijf")
        {
            context.Response.Redirect("/contact" + context.Request.QueryString, permanent: true);
            return true;
        }
        var branding = await seo.GetSettingsAsync(context.RequestAborted);
        var title = branding.ShopName; string? description = null; string? canonical = null; var preview = ""; var index = false;
        if (path.TrimEnd('/') == "/assortiment")
        {
            var settings = branding;
            title = settings.SeoTitle; description = settings.SeoDescription ?? SeoText.Summary(settings.Introduction); canonical = "/assortiment"; index = true;
            var catalog = context.RequestServices.GetRequiredService<IStorefrontCatalog>();
            var page = await catalog.ListAsync(0, 20, null, null, DateTimeOffset.UtcNow, context.RequestAborted);
            preview = "<main><p>" + E(settings.WelcomeText) + "</p><h1>" + E(settings.Heading) + "</h1><p>" + E(settings.Introduction) + "</p><ul>" + string.Concat(page.Items.Select(p =>
                "<li><a href=\"/winkel/" + E(p.WebAddress ?? p.Id.ToString("D")) + "\">" + E(p.Name) + "</a></li>")) + "</ul></main>";
        }
        else if (path.StartsWith("/assortiment/", StringComparison.Ordinal))
        {
            var navigation = context.RequestServices.GetRequiredService<MyShop.Application.Catalog.CategoryNavigation.ICategoryNavigationStore>();
            var category = await navigation.ResolvePageAsync(path[13..].TrimEnd('/'), context.RequestAborted);
            if (category is null) { context.Response.StatusCode = 404; preview = "<main><h1>Categorie niet beschikbaar</h1></main>"; }
            else
            {
                canonical = "/assortiment/" + category.WebAddress;
                if (path != canonical) { context.Response.Redirect(canonical + context.Request.QueryString, permanent: true); return true; }
                title = category.SeoTitle ?? category.Name + " · " + branding.ShopName;
                description = category.SeoDescription ?? branding.SeoDescription ?? SeoText.Summary(branding.Introduction); index = true;
                var catalog = context.RequestServices.GetRequiredService<IStorefrontCatalog>();
                var page = await catalog.ListAsync(0, 20, null, MyShop.Domain.Catalog.CategoryId.From(category.Id), DateTimeOffset.UtcNow, context.RequestAborted);
                preview = "<main><h1>" + E(category.Name) + "</h1><ul>" + string.Concat(page.Items.Select(p => "<li><a href=\"/winkel/" + E(p.WebAddress ?? p.Id.ToString("D")) + "\">" + E(p.Name) + "</a></li>")) + "</ul></main>";
            }
        }
        else if (path == "/" && branding.Home.Enabled || path.TrimEnd('/') == "/winkel/home")
        {
            var home = branding.Home.Normalize();
            title = home.SeoTitle ?? home.Heading + " · " + branding.ShopName;
            if (!home.Enabled) { context.Response.StatusCode = 404; title = "Homepagina niet beschikbaar · " + branding.ShopName; }
            else
            {
                if (path.TrimEnd('/') == "/winkel/home") { context.Response.Redirect("/" + context.Request.QueryString, permanent: true); return true; }
                canonical = "/"; index = true;
                description = home.SeoDescription ?? (home.Description is null ? null : SeoText.Summary(home.Description));
                preview = "<main><h1>" + E(branding.WelcomeText) + "</h1>" + (home.Description is null ? "" : "<section><p>" + E(home.Description) + "</p></section>") +
                    "<a href=\"/assortiment\">Bekijk het assortiment</a></main>";
            }
        }
        else if (path.TrimEnd('/') == "/contact")
        {
            var company = branding.Company;
            title = company.SeoTitle ?? company.Heading + " · " + branding.ShopName; description = company.SeoDescription ?? (company.Description is null ? null : SeoText.Summary(company.Description));
            canonical = "/contact"; index = true;
            preview = "<main><h1>" + E(company.Heading) + "</h1><h2>" + E(company.Name ?? branding.ShopName) + "</h2><p>" + E(company.Description ?? "") + "</p><dl>" +
                Contact("Adres", company.Address) + Contact("E-mailadres", company.Email) + Contact("Telefoonnummer", company.Phone) + Contact("Openingstijden", company.OpeningHours) + "</dl></main>";
        }
        else if (path.StartsWith("/winkel/", StringComparison.Ordinal) && path[8..].IndexOf('/') < 0)
        {
            var key = path[8..];
            if (!new[] { "betaling", "account", "registreren", "inloggen", "winkelmand", "e-mail-bevestigen", "wachtwoord-herstellen", "wachtwoord-vergeten" }.Contains(key))
            {
                var id = await seo.ResolveProductAsync(key, context.RequestAborted);
                var product = id is null ? null : await seo.GetProductAsync(id.Value, context.RequestAborted);
                if (product is null || !product.Published)
                { context.Response.StatusCode = 404; title = "Product niet beschikbaar · " + branding.ShopName; }
                else
                {
                    canonical = "/winkel/" + product.ResolvedAddress;
                    if (path != canonical)
                    { context.Response.Redirect(canonical + context.Request.QueryString, permanent: true); return true; }
                    title = product.ResolvedTitle; description = product.ResolvedDescription; index = true;
                    var catalog = context.RequestServices.GetRequiredService<IStorefrontCatalog>();
                    var detail = await catalog.GetAsync(MyShop.Domain.Catalog.ProductId.From(product.ProductId), context.RequestAborted);
                    preview = "<main><h1>" + E(product.Name) + "</h1><h2>" + E(product.ResolvedAboutHeading) + "</h2><p>" + E(product.Description) + "</p>" + ((detail?.Attributes.Count ?? 0) > 0 ? "<h3>" + E(product.ResolvedAttributesHeading) + "</h3>" : "") + "<dl>" +
                        string.Concat((detail?.Attributes ?? []).Select(a => "<dt>" + E(a.Name) + "</dt><dd>" + E(a.Value) + "</dd>")) + "</dl></main>";
                }
            }
        }
        if (path.Length > 1 && path.TrimEnd('/')[1..].IndexOf('/') < 0 && !new[] { "/inloggen", "/geen-toegang", "/beheer", "/producten", "/producttypen", "/categorieen", "/bestellingen", "/klanten", "/account", "/assortiment", "/contact" }.Contains(path.TrimEnd('/')))
        {
            var page = branding.Home.Enabled ? branding.Home.Normalize().Items.FirstOrDefault(item => "/" + item.WebAddress == path.TrimEnd('/')) : null;
            if (page is null) { context.Response.StatusCode = 404; title = "Pagina niet beschikbaar · " + branding.ShopName; }
            else
            {
                title = page.SeoTitle ?? (page.Title ?? branding.Home.Heading) + (page.Place is null ? "" : " in " + page.Place) + " · " + branding.ShopName;
                canonical = "/" + page.WebAddress;
                if (path != canonical) { context.Response.Redirect(canonical + context.Request.QueryString, permanent: true); return true; }
                description = page.SeoDescription ?? SeoText.Summary(page.Description); index = true;
                preview = "<main><a href=\"/\">Home</a><h1>" + E(page.Title ?? branding.Home.Heading) + "</h1><p>" + E(page.Description) + "</p></main>";
            }
        }
        if (index)
        {
            var links = (branding.Home.Enabled ? branding.Home.Normalize().Items : []).Select((item, position) =>
                "<a href=\"/" + E(item.WebAddress!) + "\">" + E(item.Title ?? $"Pagina {position + 1}") + "</a>");
            var navigation = context.RequestServices.GetRequiredService<MyShop.Application.Catalog.CategoryNavigation.ICategoryNavigationStore>();
            var showAssortment = (await navigation.GetShopAsync(context.RequestAborted)).ShowAssortmentInMenu;
            var categories = await navigation.ListVisibleAsync(context.RequestAborted);
            var categoryLinks = categories.Select(category => "<a href=\"/assortiment/" + E(category.WebAddress ?? category.Id.ToString("D")) + "\">" + E(category.Name) + "</a>");
            preview = "<nav aria-label=\"Winkelnavigatie\">" + (branding.Home.Enabled ? "<a href=\"/\">Home</a> " : "") + string.Join(" ", links) + (showAssortment ? " <a href=\"/assortiment\">Assortiment</a> " : " ") + string.Join(" ", categoryLinks) + "</nav>" + preview;
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
    private static string Contact(string label, string? value) => value is null ? "" : "<dt>" + E(label) + "</dt><dd>" + E(value) + "</dd>";
    private static string E(string value) => WebUtility.HtmlEncode(value);
}
