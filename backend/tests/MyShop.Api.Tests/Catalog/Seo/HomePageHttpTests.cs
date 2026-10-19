using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Seo;

public sealed class HomePageHttpTests
{
    [SecuritySqlFact]
    public async Task Home_is_optional_validated_ordered_persistent_and_revision_protected()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/shop/home")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/seo-settings")).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();
        var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        var originalCompany = settings["company"]!.ToJsonString();
        settings["home"] = JsonNode.Parse("""{"enabled":true,"heading":"Garage abc","items":[]}""");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        settings["home"]!["items"] = JsonNode.Parse("""[{"title":"Over Garage abc","description":"Al 20 jaar.\nReparaties en APK."},{"title":null,"description":"ma - vr 8:00 - 17:00 uur"}]""");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
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
    }
}
