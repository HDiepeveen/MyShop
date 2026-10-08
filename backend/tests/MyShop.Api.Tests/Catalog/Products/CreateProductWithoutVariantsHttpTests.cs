using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class CreateProductWithoutVariantsHttpTests
{
    [SecuritySqlFact]
    public async Task Create_without_variant_round_trips_and_can_add_first_variant_later()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var typeResponse = await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Shirt" });
        Assert.Equal(HttpStatusCode.Created, typeResponse.StatusCode);
        var type = await typeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var response = await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = type.GetProperty("id").GetGuid(), name = "Cotton shirt" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, created.GetProperty("initialVariantId").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("initialVariantName").ValueKind);
        var path = $"/api/products/{created.GetProperty("id").GetGuid()}";
        var detail = await host.Client.GetFromJsonAsync<JsonElement>(path);
        Assert.Empty(detail.GetProperty("variants").EnumerateArray());
        Assert.False(detail.GetProperty("presentation").GetProperty("isPublished").GetBoolean());
        Assert.Equal(created.GetProperty("revision").GetGuid(), detail.GetProperty("revision").GetGuid());
        var list = await host.Client.GetFromJsonAsync<JsonElement>("/api/products");
        Assert.Equal(0, list.GetProperty("items")[0].GetProperty("variantCount").GetInt32());
        Assert.Equal(HttpStatusCode.Created, (await host.Client.PostAsJsonAsync(path + "/variants", new { name = "Blue / M" })).StatusCode);
        detail = await host.Client.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal("Blue / M", Assert.Single(detail.GetProperty("variants").EnumerateArray()).GetProperty("name").GetString());
    }
}
