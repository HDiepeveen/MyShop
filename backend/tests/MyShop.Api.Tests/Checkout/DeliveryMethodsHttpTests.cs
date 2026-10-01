using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Checkout;

public sealed class DeliveryMethodsHttpTests
{
    [SecuritySqlFact]
    public async Task AdministratorControlsPublicDeliveryMethods()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient(new HttpClientHandler { UseCookies = false,
            AllowAutoRedirect = false }) { BaseAddress = host.Client.BaseAddress };
        var defaults = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/delivery-methods");
        Assert.Equal("Standaardbezorging", Assert.Single(defaults.EnumerateArray())
            .GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await visitor.GetAsync("/api/delivery-methods")).StatusCode);

        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var createdResponse = await host.Client.PostAsJsonAsync("/api/delivery-methods", new
        {
            name = "Avondbezorging", description = "Na 18:00", amount = "4.95",
            currency = "eur", enabled = true
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("4.95", created.GetProperty("amount").GetString());
        Assert.Equal("EUR", created.GetProperty("currency").GetString());

        var updatedResponse = await host.Client.PutAsJsonAsync(
            $"/api/delivery-methods/{created.GetProperty("id").GetGuid()}", new
            {
                name = "Avondbezorging", description = "Tussen 18:00 en 22:00", amount = "5.50",
                currency = "EUR", enabled = false,
                revision = created.GetProperty("revision").GetGuid()
            });
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(updated.GetProperty("enabled").GetBoolean());
        var stale = await host.Client.PutAsJsonAsync(
            $"/api/delivery-methods/{created.GetProperty("id").GetGuid()}", new
            {
                name = "Avondbezorging", amount = "6.00", currency = "EUR", enabled = true,
                revision = created.GetProperty("revision").GetGuid()
            });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var publicMethods = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/delivery-methods");
        Assert.DoesNotContain(publicMethods.EnumerateArray(), method =>
            method.GetProperty("id").GetGuid() == created.GetProperty("id").GetGuid());

        var deleted = await host.Client.DeleteAsync(
            $"/api/delivery-methods/{created.GetProperty("id").GetGuid()}?revision={updated.GetProperty("revision").GetGuid()}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }
}
