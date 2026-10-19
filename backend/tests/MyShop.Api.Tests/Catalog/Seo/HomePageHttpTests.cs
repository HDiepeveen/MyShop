using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MyShop.Api.Tests.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Catalog.Seo;

public sealed class HomePageHttpTests
{
    [SecuritySqlFact]
    public async Task Common_legacy_place_is_adopted_and_global_changes_preserve_addresses()
    {
        await using var host = await SecurityHost.Create();
        var legacy = """{"Enabled":true,"Heading":"Garage","Items":[{"Title":"APK","Place":"Zwolle","WebAddress":"apk-zwolle","Description":"Keuring."},{"Title":"Onderhoud","Place":"Zwolle","WebAddress":"onderhoud-zwolle","Description":"Onderhoud."}]}""";
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE CatalogSeoSettings SET HomePageJson={legacy}");
        }
        await host.Csrf(); await host.Login(); await host.Csrf();
        var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal("Zwolle", settings["home"]!["place"]!.GetValue<string>());
        settings["home"]!["place"] = "Utrecht";
        settings["home"]!["description"] = "Onze eigen homepagina.";
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var home = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/home"))!;
        Assert.Equal("Utrecht", home["place"]!.GetValue<string>());
        Assert.Equal("Onze eigen homepagina.", home["description"]!.GetValue<string>());
        Assert.All(home["items"]!.AsArray(), item => Assert.Equal("Utrecht", item!["place"]!.GetValue<string>()));
        Assert.Equal("apk-zwolle", home["items"]![0]!["webAddress"]!.GetValue<string>());
        settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        settings["home"]!["place"] = "";
        settings["home"]!["items"] = new JsonArray();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var onlyHome = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/home"))!;
        Assert.Null(onlyHome["place"]);
        Assert.Empty(onlyHome["items"]!.AsArray());
    }

    [SecuritySqlFact]
    public async Task Home_is_optional_validated_ordered_persistent_and_revision_protected()
    {
        var folder = Path.Combine(Path.GetTempPath(), "MyShopPages_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "index.html"), "<html><head><title>MyShop</title></head><body><app-root></app-root></body></html>");
        try {
        await using var host = await SecurityHost.Create(new() { ["Hosting:FrontendRoot"] = folder });
        using var visitor = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/shop/home")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/seo-settings")).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();
        var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        var originalCompany = settings["company"]!.ToJsonString();
        settings["home"] = JsonNode.Parse("""{"enabled":true,"heading":" ","items":[]}""");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        settings["home"]!["heading"] = "Garage abc";
        settings["home"]!["place"] = "Zwolle";
        settings["home"]!["description"] = "Welkom bij onze garage.\nAl twintig jaar persoonlijke service.";
        settings["home"]!["items"] = JsonNode.Parse("""[{"title":"APK","place":"Zwolle","description":"Al 20 jaar.\nReparaties en APK."},{"title":null,"description":"ma - vr 8:00 - 17:00 uur"}]""");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var root = await visitor.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, root.StatusCode);
        var html = await root.Content.ReadAsStringAsync();
        var nav = html[html.IndexOf("<nav", StringComparison.Ordinal)..html.IndexOf("</nav>", StringComparison.Ordinal)];
        Assert.True(nav.IndexOf(">Home</a>", StringComparison.Ordinal) < nav.IndexOf(">APK</a>", StringComparison.Ordinal));
        Assert.True(nav.IndexOf(">APK</a>", StringComparison.Ordinal) < nav.IndexOf(">Assortiment</a>", StringComparison.Ordinal));
        Assert.Contains("href=\"/apk-zwolle\"", nav);
        Assert.DoesNotContain("<main><a href=\"/apk-zwolle\"", html);
        var publicSettings = (await visitor.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!;
        Assert.Equal("apk-zwolle", publicSettings["homePages"]![0]!["webAddress"]!.GetValue<string>());
        Assert.Contains("Welkom bij onze garage.", html);
        Assert.DoesNotContain("ma - vr 8:00 - 17:00 uur", html);
        Assert.DoesNotContain("Reparaties en APK.", html);
        Assert.DoesNotContain("name=\"robots\" content=\"noindex\"", html);
        var oldHome = await visitor.GetAsync("/winkel/home");
        Assert.Equal(HttpStatusCode.MovedPermanently, oldHome.StatusCode);
        Assert.Equal("/", oldHome.Headers.Location!.OriginalString);
        var landing = await visitor.GetAsync("/apk-zwolle");
        Assert.Equal(HttpStatusCode.OK, landing.StatusCode);
        Assert.Contains("<h1>APK</h1>", await landing.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/onbekende-dienst")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/orders")).StatusCode);
        var home = (await visitor.GetFromJsonAsync<JsonObject>("/api/shop/home"))!;
        Assert.Equal("Garage abc", home["heading"]!.GetValue<string>());
        Assert.Equal("Al 20 jaar.\nReparaties en APK.", home["items"]![0]!["description"]!.GetValue<string>());
        Assert.Null(home["items"]![1]!["title"]);
        var current = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal(originalCompany, current["company"]!.ToJsonString());
        current["home"]!["items"]![0]!["description"] = " ";
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings", current)).StatusCode);
        current["home"]!["items"]![0]!["description"] = "Al 20 jaar.";
        current["home"]!["enabled"] = false;
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", current)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/shop/home")).StatusCode);
        var draft = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal(2, draft["home"]!["items"]!.AsArray().Count);
        Assert.Empty((await visitor.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!["homePages"]!.AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/shop/pages/apk-zwolle")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/apk-zwolle")).StatusCode);
        } finally { Directory.Delete(folder, recursive: true); }
    }
}
