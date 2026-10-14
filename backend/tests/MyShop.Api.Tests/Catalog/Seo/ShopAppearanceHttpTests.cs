using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Seo;

public sealed class ShopAppearanceHttpTests
{
    [SecuritySqlFact]
    public async Task AppearanceIsProtectedValidatedPersistentAndDoesNotOverwriteOtherSettings()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/shop-appearance")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PutAsJsonAsync("/api/shop-appearance", new { })).StatusCode);
        var defaults = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        Assert.Equal("#1d5543", defaults.GetProperty("appearance").GetProperty("primaryColor").GetString());
        Assert.False(defaults.GetProperty("showManagementMenuWithoutLogin").GetBoolean());

        await host.Csrf(); await host.Login(); await host.Csrf();
        var seo = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        var initial = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop-appearance");
        var revision = initial.GetProperty("revision").GetGuid();
        var input = new { revision, showManagementMenuWithoutLogin = true, colors = new { primaryColor = "#A7D9FF", backgroundColor = "#111827", panelColor = "#1f2937", textColor = "#f3f4f6" } };
        host.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/shop-appearance", input)).StatusCode);
        await host.Csrf();
        foreach (var invalid in new[] { "#fff", "red", "#123456\n", "#123456; background:url(evil)", "" })
        {
            var bad = JsonNode.Parse(JsonSerializer.Serialize(input))!;
            bad["colors"]!["primaryColor"] = invalid;
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/shop-appearance", bad)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/shop-appearance", input)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/shop-appearance", input)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/seo-settings", seo)).StatusCode);
        var saved = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop-appearance");
        Assert.NotEqual(revision, saved.GetProperty("revision").GetGuid());
        Assert.Equal("#a7d9ff", saved.GetProperty("colors").GetProperty("primaryColor").GetString());
        var currentSeo = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        Assert.Equal(seo.GetProperty("shopName").GetString(), currentSeo.GetProperty("shopName").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", currentSeo)).StatusCode);
        var publicSettings = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        Assert.Equal("#111827", publicSettings.GetProperty("appearance").GetProperty("backgroundColor").GetString());
        Assert.False(publicSettings.TryGetProperty("revision", out _));
        Assert.True(publicSettings.GetProperty("showManagementMenuWithoutLogin").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/orders")).StatusCode);

        await host.Client.PostAsJsonAsync("/api/auth/logout", new { });
        await host.Csrf();
        await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "appearance-customer@example.test", password = SecurityHost.Password });
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/shop-appearance")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync("/api/shop-appearance", input)).StatusCode);
    }
}
