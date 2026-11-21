using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HDWebFramework.Api.Tests.Security;
using HDWebFramework.Application.Catalog.OverheidIo;
using HDWebFramework.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HDWebFramework.Api.Tests.Catalog;

public sealed class OverheidIoHttpTests
{
    [SecuritySqlFact]
    public async Task ImportsPersistWithSelectedFieldsAndOnlyPublishedVehiclesArePublic()
    {
        await using var host = await SecurityHost.Create(configureServices: services =>
            services.AddSingleton<IOverheidIoClient>(new Client()));
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/overheid-io-settings")).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();

        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/overheid-io-settings");
        Assert.Contains(settings.GetProperty("columns").EnumerateArray(), c => c.GetProperty("key").GetString() == "brandstof.brandstofverbruik_gecombineerd");
        Assert.False(settings.GetProperty("settings").GetProperty("hasApiKey").GetBoolean());
        var revision = settings.GetProperty("settings").GetProperty("revision").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/overheid-io-settings",
            new { revision, columns = new[] { "unknown" } })).StatusCode);
        var selection = new { revision, columns = new[] { "merk", "vervaldatum_apk" }, apiKey = "integration-test-key" };
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/overheid-io-settings", selection)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/overheid-io-settings", selection)).StatusCode);
        var publicSettings = await host.Client.GetAsync("/api/overheid-io-settings");
        Assert.DoesNotContain("integration-test-key", await publicSettings.Content.ReadAsStringAsync());
        using (var scope = host.App.Services.CreateScope())
        {
            Assert.Equal("integration-test-key", await scope.ServiceProvider.GetRequiredService<IOverheidIoSettingsStore>().GetApiKeyAsync(default));
            var db = scope.ServiceProvider.GetRequiredService<HDWebFrameworkDbContext>();
            await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT ProtectedApiKey FROM OverheidIoSettings";
            var encrypted = (string)(await command.ExecuteScalarAsync())!;
            Assert.DoesNotContain("integration-test-key", encrypted);
        }


        var typeResponse = await host.Client.PostAsJsonAsync("/api/product-types", new { name = "RDW test" });
        Assert.Equal(HttpStatusCode.Created, typeResponse.StatusCode);
        var typeId = (await typeResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var attrResponse = await host.Client.PostAsJsonAsync($"/api/product-types/{typeId}/attributes",
            new { code = "rdw", displayName = "Kenteken", dataType = 8, isRequired = false, isFilterable = false, scope = 0 });
        Assert.Equal(HttpStatusCode.Created, attrResponse.StatusCode);
        var attributeId = (await attrResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var productResponse = await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = typeId, name = "RDW auto" });
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync(
            $"/api/products/{productId}/overheid-io/{attributeId}", new { licensePlate = "ab-12-cd" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{productId}/vehicle-data")).StatusCode);
        var product = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal("AB12CD", product.GetProperty("attributeValues")[0].GetProperty("value").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{productId}/presentation",
            new { description = "", imageUrl = (string?)null, imageAlt = "", isPublished = true,
                revision = product.GetProperty("revision").GetGuid() })).StatusCode);
        var vehicles = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{productId}/vehicle-data");
        var fields = vehicles[0].GetProperty("fields");
        Assert.Equal(2, fields.GetArrayLength());
        Assert.Equal("OPEL", fields[0].GetProperty("value").GetString());
        Assert.Equal("18-11-2027", fields[1].GetProperty("value").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PostAsJsonAsync(
            $"/api/products/{productId}/overheid-io/{attributeId}", new { licensePlate = "ZZ99ZZ" })).StatusCode);
        var unchanged = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{productId}/vehicle-data");
        Assert.Equal("AB12CD", unchanged[0].GetProperty("licensePlate").GetString());
        settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/overheid-io-settings");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/overheid-io-settings",
            new { revision = settings.GetProperty("settings").GetProperty("revision").GetGuid(),
                columns = new[] { "brandstof.brandstof_omschrijving", "brandstof.brandstofverbruik_gecombineerd", "brandstof.co2_uitstoot_gecombineerd" } })).StatusCode);
        vehicles = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{productId}/vehicle-data");
        var fuelFields = vehicles[0].GetProperty("fields").EnumerateArray()
            .ToDictionary(f => f.GetProperty("key").GetString()!, f => f.GetProperty("value").GetString());
        Assert.Equal(3, fuelFields.Count);
        Assert.Equal("Benzine", fuelFields["brandstof.brandstof_omschrijving"]);
        Assert.Equal("6.2", fuelFields["brandstof.brandstofverbruik_gecombineerd"]);
        Assert.Equal("140", fuelFields["brandstof.co2_uitstoot_gecombineerd"]);
        using (var scope = host.App.Services.CreateScope())
            Assert.Equal("integration-test-key", await scope.ServiceProvider.GetRequiredService<IOverheidIoSettingsStore>().GetApiKeyAsync(default));
        settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/overheid-io-settings");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/overheid-io-settings", new {
            revision = settings.GetProperty("settings").GetProperty("revision").GetGuid(),
            columns = new[] { "merk" }, clearApiKey = true
        })).StatusCode);
        using (var scope = host.App.Services.CreateScope())
            Assert.Null(await scope.ServiceProvider.GetRequiredService<IOverheidIoSettingsStore>().GetApiKeyAsync(default));
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.DeleteAsync($"/api/products/{productId}/attributes/{attributeId}")).StatusCode);
        product = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Empty(product.GetProperty("attributeValues").EnumerateArray());
        vehicles = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{productId}/vehicle-data");
        Assert.Empty(vehicles.EnumerateArray());



    }

    private sealed class Client : IOverheidIoClient
    {
        public Task<string?> FindAsync(string plate, CancellationToken ct) =>
            Task.FromResult(plate == "AB12CD" ? """{"kenteken":"AB12CD","merk":"OPEL","vervaldatum_apk":"2027-11-18","eerste_kleur":"GRIJS","brandstof.brandstof_omschrijving":"Benzine","brandstof.brandstofverbruik_gecombineerd":"6.2","brandstof.co2_uitstoot_gecombineerd":"140"}""" : null);
    }
}
