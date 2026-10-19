using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Seo;

public sealed class PageSeoHttpTests
{
    [SecuritySqlFact]
    public async Task Page_metadata_is_persisted_escaped_and_optional_without_changing_visible_content()
    {
        var folder = Path.Combine(Path.GetTempPath(), "MyShopPageSeo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "index.html"), "<html><head><title>MyShop</title></head><body><app-root></app-root></body></html>");
        try
        {
            await using var host = await SecurityHost.Create(new() { ["Hosting:FrontendRoot"] = folder });
            await host.Csrf(); await host.Login(); await host.Csrf();
            var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
            settings["shopName"] = "Garage";
            settings["introduction"] = "Ons assortiment.";
            settings["home"] = JsonNode.Parse("""{"enabled":true,"heading":"Home kop","description":"Home inhoud.","place":"Zwolle","items":[{"title":"APK","description":"APK inhoud."}]}""");
            settings["company"]!["heading"] = "Contact";
            settings["company"]!["description"] = "Contact inhoud.";
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
            using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
            foreach (var custom in new[] { true, false })
            {
                settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
                settings["seoTitle"] = custom ? " Eigen <SEO> & titel " : "Assortiment · Garage";
                settings["seoDescription"] = custom ? " Eigen omschrijving met \"aanhaling\" & tekst. " : "";
                var targets = new[] { settings["home"]!, settings["home"]!["items"]![0]!, settings["company"]! };
                foreach (var target in targets)
                {
                    target["seoTitle"] = custom ? "  Eigen <SEO> & titel  " : "   ";
                    target["seoDescription"] = custom ? " Eigen omschrijving met \"aanhaling\" & tekst. " : "";
                }
                Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
                var stored = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
                foreach (var target in new[] { stored["home"]!, stored["home"]!["items"]![0]!, stored["company"]! })
                {
                    Assert.Equal(custom ? "Eigen <SEO> & titel" : null, target["seoTitle"]?.GetValue<string>());
                    Assert.Equal(custom ? "Eigen omschrijving met \"aanhaling\" & tekst." : null, target["seoDescription"]?.GetValue<string>());
                }
                foreach (var (url, automaticTitle, text) in new[] { ("/winkel", "Assortiment · Garage", "Ons assortiment."), ("/", "Home kop · Garage", "Home inhoud."), ("/apk", "APK in Zwolle · Garage", "APK inhoud."), ("/winkel/informatie/bedrijf", "Contact · Garage", "Contact inhoud.") })
                {
                    var html = await visitor.GetStringAsync(url);
                    Assert.Contains("<title>" + WebUtility.HtmlEncode(custom ? "Eigen <SEO> & titel" : automaticTitle) + "</title>", html);
                    Assert.Contains("<meta name=\"description\" content=\"" + WebUtility.HtmlEncode(custom ? "Eigen omschrijving met \"aanhaling\" & tekst." : text) + "\">", html);
                    Assert.Contains(text, html);
                    Assert.DoesNotContain("<h1>Eigen", html);
                }
                Assert.Equal(custom ? "Eigen <SEO> & titel" : null, (await visitor.GetFromJsonAsync<JsonObject>("/api/shop/home"))!["seoTitle"]?.GetValue<string>());
                Assert.Equal(custom ? "Eigen <SEO> & titel" : null, (await visitor.GetFromJsonAsync<JsonObject>("/api/shop/pages/apk"))!["seoTitle"]?.GetValue<string>());
                Assert.Equal(custom ? "Eigen <SEO> & titel" : null, (await visitor.GetFromJsonAsync<JsonObject>("/api/shop/company"))!["seoTitle"]?.GetValue<string>());
            }
            foreach (var area in new[] { "home", "page", "company", "shop" })
            foreach (var field in new[] { "seoTitle", "seoDescription" })
            {
                settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
                var target = area == "shop" ? settings : area == "page" ? settings["home"]!["items"]![0]! : settings[area]!;
                target[field] = new string('x', field == "seoTitle" ? 201 : 501);
                Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
                var unchanged = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
                Assert.Equal(settings["revision"]!.GetValue<string>(), unchanged["revision"]!.GetValue<string>());
            }
        }
        finally { Directory.Delete(folder, true); }
    }
}
