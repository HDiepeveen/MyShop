using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class ProductPresentationHttpTests
{
    [SecuritySqlFact]
    public async Task PresentationRoundTripsWithPublicationAndConcurrencyProtection()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var typeResponse = await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Clothing" });
        Assert.Equal(HttpStatusCode.Created, typeResponse.StatusCode);
        var type = await typeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productResponse = await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = type.GetProperty("id").GetGuid(), name = "Shirt", initialVariantName = "Default" });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var created = await productResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        var revision = created.GetProperty("revision").GetGuid();
        var path = $"/api/products/{id}";
        var detail = await host.Client.GetFromJsonAsync<JsonElement>(path);
        Assert.False(detail.GetProperty("presentation").GetProperty("isPublished").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync(path + "/presentation", new { description = "", imageUrl = (string?)null, imageAlt = "", isPublished = true, revision })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(path + "/presentation", new { description = "Linen shirt\nComfortable.", imageUrl = "https://example.com/shirt.jpg", imageAlt = "A linen shirt", isPublished = true, revision })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync(path + "/presentation", new { description = "stale", imageUrl = (string?)null, imageAlt = "", isPublished = false, revision })).StatusCode);
        await host.Client.PatchAsJsonAsync(path + "/name", new { name = "Renamed shirt" });
        detail = await host.Client.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal("Linen shirt\nComfortable.", detail.GetProperty("presentation").GetProperty("description").GetString());
        Assert.True(detail.GetProperty("presentation").GetProperty("isPublished").GetBoolean());
        Assert.Equal("https://example.com/shirt.jpg", detail.GetProperty("presentation").GetProperty("imageUrl").GetString());
        var list = await host.Client.GetFromJsonAsync<JsonElement>("/api/products");
        Assert.True(list.GetProperty("items")[0].GetProperty("isPublished").GetBoolean());
        revision = detail.GetProperty("revision").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(path + "/presentation", new { description = "", imageUrl = (string?)null, imageAlt = "", isPublished = false, revision })).StatusCode);
        detail = await host.Client.GetFromJsonAsync<JsonElement>(path);
        Assert.False(detail.GetProperty("presentation").GetProperty("isPublished").GetBoolean());
        Assert.Equal("Renamed shirt", detail.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}/presentation", new { description = "", imageUrl = (string?)null, imageAlt = "", isPublished = false, revision })).StatusCode);
    }
}
