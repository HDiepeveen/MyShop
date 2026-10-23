using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HDWebFramework.Api.Tests.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Infrastructure.Persistence;

namespace HDWebFramework.Api.Tests.Catalog.Seo;

public sealed class HomePageHttpTests
{
    [SecuritySqlFact]
    public async Task Contact_can_be_disabled_and_navigation_order_is_persisted()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); await host.Login(); await host.Csrf();
        var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.True(settings["home"]!["contactEnabled"]!.GetValue<bool>());
        settings["home"]!["contactEnabled"] = false;
        settings["home"]!["menuOrder"] = new JsonArray("contact", "home", "assortment");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var stored = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.False(stored["home"]!["contactEnabled"]!.GetValue<bool>());
        var navigation = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!;
        Assert.False(navigation["contactEnabled"]!.GetValue<bool>());
        Assert.Equal("contact", navigation["menuOrder"]![0]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/shop/company")).StatusCode);
        stored["home"]!["contactEnabled"] = true;
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", stored)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/shop/company")).StatusCode);
    }

    [SecuritySqlFact]
    public async Task Common_legacy_place_is_adopted_and_global_changes_update_addresses()
    {
        await using var host = await SecurityHost.Create();
        var legacy = """{"Enabled":true,"Heading":"Garage","Items":[{"Title":"APK","Place":"Zwolle","WebAddress":"apk-zwolle","Description":"Keuring."},{"Title":"Onderhoud","Place":"Zwolle","WebAddress":"onderhoud-zwolle","Description":"Onderhoud."}]}""";
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE CatalogSeoSettings SET HomePageJson={legacy}");
        }
        await host.Csrf(); await host.Login(); await host.Csrf();
        var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal("Zwolle", settings["home"]!["place"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/shop/pages/apk-zwolle")).StatusCode);
        settings["welcomeText"] = "Welkom bij onze zaak";
        settings["home"]!["place"] = "Utrecht";
        settings["home"]!["description"] = "Onze eigen homepagina.";
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var home = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/home"))!;
        Assert.Equal("Welkom bij onze zaak", home["welcomeText"]!.GetValue<string>());
        Assert.Equal("Garage", home["heading"]!.GetValue<string>());
        Assert.Equal("Utrecht", home["place"]!.GetValue<string>());
        Assert.Equal("Onze eigen homepagina.", home["description"]!.GetValue<string>());
        Assert.All(home["items"]!.AsArray(), item => Assert.Equal("Utrecht", item!["place"]!.GetValue<string>()));
        Assert.Equal("apk", home["items"]![0]!["webAddress"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/shop/pages/apk-zwolle")).StatusCode);
        settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        settings["home"]!["items"]![0]!["title"] = "Reparatie";
        settings["home"]!["items"]![0]!["seoTitle"] = "Onze reparaties";
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/shop/pages/apk")).StatusCode);
        var renamed = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/pages/reparatie"))!;
        Assert.Equal("Reparatie", renamed["title"]!.GetValue<string>());
        Assert.Equal("Onze reparaties", renamed["seoTitle"]!.GetValue<string>());
        var navigation = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!;
        Assert.Equal("reparatie", navigation["homePages"]![0]!["webAddress"]!.GetValue<string>());
        foreach (var title in new string?[] { null, "", "   " })
        {
            var invalid = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
            invalid["home"]!["items"]![0]!["title"] = title;
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings", invalid)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/shop/pages/reparatie")).StatusCode);
        }
        settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        settings["home"]!["place"] = "";
        settings["home"]!["items"] = new JsonArray();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var onlyHome = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/home"))!;
        Assert.Null(onlyHome["place"]);
        Assert.Empty(onlyHome["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/shop/pages/reparatie")).StatusCode);
    }

    [SecuritySqlFact]
    public async Task Home_is_optional_validated_ordered_persistent_and_revision_protected()
    {
        var folder = Path.Combine(Path.GetTempPath(), "HDWebFrameworkPages_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "index.html"), "<html><head><title>HDWebFramework</title></head><body><app-root></app-root></body></html>");
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
        settings["home"]!["items"] = JsonNode.Parse("""[{"title":"APK","place":"Zwolle","description":"Al 20 jaar.\nReparaties en APK."},{"title":"Openingstijden","description":"ma - vr 8:00 - 17:00 uur"}]""");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var root = await visitor.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, root.StatusCode);
        var html = await root.Content.ReadAsStringAsync();
        var nav = html[html.IndexOf("<nav", StringComparison.Ordinal)..html.IndexOf("</nav>", StringComparison.Ordinal)];
        Assert.True(nav.IndexOf(">Home</a>", StringComparison.Ordinal) < nav.IndexOf(">APK</a>", StringComparison.Ordinal));
        Assert.True(nav.IndexOf(">APK</a>", StringComparison.Ordinal) < nav.IndexOf(">Assortiment</a>", StringComparison.Ordinal));
        Assert.Contains("href=\"/apk\"", nav);
        Assert.DoesNotContain("<main><a href=\"/apk\"", html);
        var publicSettings = (await visitor.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!;
        Assert.Equal("apk", publicSettings["homePages"]![0]!["webAddress"]!.GetValue<string>());
        Assert.Contains("Welkom bij onze garage.", html);
        Assert.DoesNotContain("ma - vr 8:00 - 17:00 uur", html);
        Assert.DoesNotContain("Reparaties en APK.", html);
        Assert.DoesNotContain("name=\"robots\" content=\"noindex\"", html);
        var oldHome = await visitor.GetAsync("/winkel/home");
        Assert.Equal(HttpStatusCode.MovedPermanently, oldHome.StatusCode);
        Assert.Equal("/", oldHome.Headers.Location!.OriginalString);
        var landing = await visitor.GetAsync("/apk");
        Assert.Equal(HttpStatusCode.OK, landing.StatusCode);
        Assert.Contains("<h1>APK</h1>", await landing.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/onbekende-dienst")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/orders")).StatusCode);
        var home = (await visitor.GetFromJsonAsync<JsonObject>("/api/shop/home"))!;
        Assert.Equal("Garage abc", home["heading"]!.GetValue<string>());
        Assert.Equal("Al 20 jaar.\nReparaties en APK.", home["items"]![0]!["description"]!.GetValue<string>());
        Assert.Equal("Openingstijden", home["items"]![1]!["title"]!.GetValue<string>());
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
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/shop/pages/apk")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/apk")).StatusCode);
        } finally { Directory.Delete(folder, recursive: true); }
    }
}
