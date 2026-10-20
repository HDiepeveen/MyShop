using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;
namespace MyShop.Api.Tests.Catalog.Seo;
public sealed class CategoryNavigationHttpTests
{
    [SecuritySqlFact]
    public async Task Assortment_link_defaults_visible_and_can_be_hidden_independently_with_concurrency_control()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        const string url = "/api/categories/navigation-settings";
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PutAsJsonAsync(url, new { showAssortmentInMenu = false, revision = Guid.Empty })).StatusCode);
        var branding = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        Assert.True(branding.GetProperty("showAssortmentInMenu").GetBoolean());
        await host.Csrf(); await host.Login(); await host.Csrf();
        var seo = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        var settings = await host.Client.GetFromJsonAsync<JsonElement>(url);
        var request = new { showAssortmentInMenu = false, revision = settings.GetProperty("revision").GetGuid() };
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(url, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync(url, request)).StatusCode);
        branding = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        Assert.False(branding.GetProperty("showAssortmentInMenu").GetBoolean());
        Assert.Equal(seo.GetProperty("revision").GetGuid(), (await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings")).GetProperty("revision").GetGuid());
        settings = await host.Client.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(url, new { showAssortmentInMenu = true, revision = settings.GetProperty("revision").GetGuid() })).StatusCode);
        Assert.True((await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings")).GetProperty("showAssortmentInMenu").GetBoolean());
    }
    [SecuritySqlFact]
    public async Task Menu_is_opt_in_filters_empty_categories_and_preserves_concurrency_and_renames()
    {
        await using var host = await SecurityHost.Create();
        var missing = $"/api/categories/{Guid.NewGuid()}/navigation";
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync(missing)).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync(missing)).StatusCode);
        var created = await host.Client.PostAsJsonAsync("/api/categories", new { name = "Occasions" });
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var url = $"/api/categories/{id}/navigation";
        var settings = await host.Client.GetFromJsonAsync<JsonElement>(url);
        Assert.False(settings.GetProperty("showInMenu").GetBoolean());
        var request = new { showInMenu = true, revision = settings.GetProperty("revision").GetGuid() };
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(url, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync(url, request)).StatusCode);
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        var publicSettings = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        var link = Assert.Single(publicSettings.GetProperty("categoryLinks").EnumerateArray());
        Assert.Equal(id, link.GetProperty("id").GetGuid());
        Assert.Equal(new[] { "id", "name", "webAddress" }, link.EnumerateObject().Select(x => x.Name).Order());
        var categories = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/categories");
        Assert.Contains(categories.EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);
        (await host.Client.PatchAsJsonAsync($"/api/categories/{id}/name", new { name = "Auto's" })).EnsureSuccessStatusCode();
        publicSettings = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        Assert.Equal("Auto's", publicSettings.GetProperty("categoryLinks")[0].GetProperty("name").GetString());
        settings = await host.Client.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(url, new { showInMenu = false, revision = settings.GetProperty("revision").GetGuid() })).StatusCode);
        publicSettings = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        Assert.Empty(publicSettings.GetProperty("categoryLinks").EnumerateArray());
    }
}
