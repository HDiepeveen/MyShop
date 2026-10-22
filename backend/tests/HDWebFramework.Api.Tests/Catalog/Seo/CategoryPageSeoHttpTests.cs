using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HDWebFramework.Api.Tests.Security;
namespace HDWebFramework.Api.Tests.Catalog.Seo;
public sealed class CategoryPageSeoHttpTests
{
    [SecuritySqlFact]
    public async Task Category_metadata_is_optional_unique_validated_and_rendered_with_its_own_canonical()
    {
        var folder = Path.Combine(Path.GetTempPath(), "HDWebFrameworkCategorySeo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "index.html"), "<html><head><title>HDWebFramework</title></head><body><app-root></app-root></body></html>");
        try
        {
            await using var host = await SecurityHost.Create(new() { ["Hosting:FrontendRoot"] = folder });
            using var visitor = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = host.Client.BaseAddress };
            await host.Csrf(); await host.Login(); await host.Csrf();
            async Task<Guid> Create(string name) { var response = await host.Client.PostAsJsonAsync("/api/categories", new { name }); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(); }
            var id = await Create("Occasions"); var other = await Create("Onderdelen");
            var url = $"/api/categories/{id}/navigation";
            var current = await host.Client.GetFromJsonAsync<JsonElement>(url); var revision = current.GetProperty("revision").GetGuid();
            Assert.Equal(id, (await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/category-pages/{id}")).GetProperty("id").GetGuid());
            foreach (var address in new[] { "bad/path", "has spaces", new string('x', 161), other.ToString() })
                Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync(url, new { showInMenu = true, revision, webAddress = address })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync(url, new { showInMenu = true, revision, seoTitle = new string('x', 201) })).StatusCode);
            var save = await host.Client.PutAsJsonAsync(url, new { showInMenu = true, revision, webAddress = " Occasions ", seoTitle = "Occasions <Zwolle>", seoDescription = "Bekijk onze occasions & aanbod." });
            Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync(url, new { showInMenu = true, revision, webAddress = "occasions" })).StatusCode);
            var page = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/category-pages/occasions");
            Assert.Equal(id, page.GetProperty("id").GetGuid()); Assert.Equal("Occasions <Zwolle>", page.GetProperty("seoTitle").GetString());
            var html = await visitor.GetStringAsync("/assortiment/occasions");
            Assert.Contains("<title>Occasions &lt;Zwolle&gt;</title>", html);
            Assert.Contains("content=\"Bekijk onze occasions &amp; aanbod.\"", html);
            Assert.Contains("/assortiment/occasions\"", html); Assert.Contains("<h1>Occasions</h1>", html);
            var redirect = await visitor.GetAsync($"/assortiment/{id}?search=auto"); Assert.Equal(HttpStatusCode.MovedPermanently, redirect.StatusCode); Assert.Equal("/assortiment/occasions?search=auto", redirect.Headers.Location!.OriginalString);
            var otherSettings = await host.Client.GetFromJsonAsync<JsonElement>($"/api/categories/{other}/navigation");
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync($"/api/categories/{other}/navigation", new { showInMenu = true, revision = otherSettings.GetProperty("revision").GetGuid(), webAddress = "OCCASIONS" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/shop/category-pages/missing")).StatusCode);
            var missing = await visitor.GetAsync("/assortiment/missing"); Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode); Assert.Contains("noindex", await missing.Content.ReadAsStringAsync());
            current = await host.Client.GetFromJsonAsync<JsonElement>(url);
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(url, new { showInMenu = false, revision = current.GetProperty("revision").GetGuid(), webAddress = " ", seoTitle = " ", seoDescription = " " })).StatusCode);
            page = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/category-pages/{id}"); Assert.Equal(JsonValueKind.Null, page.GetProperty("seoTitle").ValueKind);
        }
        finally { Directory.Delete(folder, true); }
    }
}
