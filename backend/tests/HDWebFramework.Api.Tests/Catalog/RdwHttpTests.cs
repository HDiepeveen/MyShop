using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HDWebFramework.Api.Tests.Security;
using HDWebFramework.Application.Catalog.Rdw;
using Microsoft.Extensions.DependencyInjection;

namespace HDWebFramework.Api.Tests.Catalog;

public sealed class RdwHttpTests
{
    [SecuritySqlFact]
    public async Task ImportsPersistWithSelectedFieldsAndOnlyPublishedVehiclesArePublic()
    {
        await using var host = await SecurityHost.Create(configureServices: services =>
            services.AddSingleton<IRdwClient>(new Client()));
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/rdw-settings")).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();

        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/rdw-settings");
        Assert.Equal(98, settings.GetProperty("columns").GetArrayLength());
        var revision = settings.GetProperty("settings").GetProperty("revision").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/rdw-settings",
            new { revision, columns = new[] { "unknown" } })).StatusCode);
        var selection = new { revision, columns = new[] { "merk", "vervaldatum_apk" } };
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/rdw-settings", selection)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/rdw-settings", selection)).StatusCode);

        var typeResponse = await host.Client.PostAsJsonAsync("/api/product-types", new { name = "RDW test" });
        Assert.Equal(HttpStatusCode.Created, typeResponse.StatusCode);
        var typeId = (await typeResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var attrResponse = await host.Client.PostAsJsonAsync($"/api/product-types/{typeId}/attributes",
            new { code = "rdw", displayName = "Kenteken", dataType = 7, isRequired = false, isFilterable = false, scope = 0 });
        Assert.Equal(HttpStatusCode.Created, attrResponse.StatusCode);
        var attributeId = (await attrResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var productResponse = await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = typeId, name = "RDW auto" });
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync(
            $"/api/products/{productId}/rdw/{attributeId}", new { licensePlate = "ab-12-cd" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{productId}/rdw")).StatusCode);
        var product = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal("AB12CD", product.GetProperty("attributeValues")[0].GetProperty("value").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{productId}/presentation",
            new { description = "", imageUrl = (string?)null, imageAlt = "", isPublished = true,
                revision = product.GetProperty("revision").GetGuid() })).StatusCode);
        var vehicles = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{productId}/rdw");
        var fields = vehicles[0].GetProperty("fields");
        Assert.Equal(2, fields.GetArrayLength());
        Assert.Equal("OPEL", fields[0].GetProperty("value").GetString());
        Assert.Equal("18-11-2027", fields[1].GetProperty("value").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PostAsJsonAsync(
            $"/api/products/{productId}/rdw/{attributeId}", new { licensePlate = "ZZ99ZZ" })).StatusCode);
        var unchanged = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{productId}/rdw");
        Assert.Equal("AB12CD", unchanged[0].GetProperty("licensePlate").GetString());
    }

    private sealed class Client : IRdwClient
    {
        public Task<string?> FindAsync(string plate, CancellationToken ct) =>
            Task.FromResult(plate == "AB12CD" ? """{"kenteken":"AB12CD","merk":"OPEL","vervaldatum_apk":"20271118","eerste_kleur":"GRIJS"}""" : null);
    }
}
