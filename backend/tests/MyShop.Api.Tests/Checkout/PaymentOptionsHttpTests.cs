using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Checkout;

public sealed class PaymentOptionsHttpTests
{
    [SecuritySqlFact]
    public async Task AdministratorControlsThePubliclyAvailablePaymentMethods()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient(new HttpClientHandler
        {
            UseCookies = false,
            AllowAutoRedirect = false
        }) { BaseAddress = host.Client.BaseAddress };

        var publicOptions = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/payment-options");
        var option = Assert.Single(publicOptions.GetProperty("items").EnumerateArray());
        Assert.Equal("payLater", option.GetProperty("code").GetString());
        Assert.Equal(new[] { "code", "name" }, option.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/payment-options")).StatusCode);

        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.True(settings.GetProperty("payLaterEnabled").GetBoolean());
        Assert.False(settings.GetProperty("onlinePaymentEnabled").GetBoolean());
        Assert.False(settings.GetProperty("onlinePaymentConfigured").GetBoolean());
        var revision = settings.GetProperty("revision").GetGuid();

        var none = await host.Client.PutAsJsonAsync("/api/payment-options", new
        {
            payLaterEnabled = false,
            onlinePaymentEnabled = false,
            revision
        });
        Assert.Equal(HttpStatusCode.BadRequest, none.StatusCode);
        Assert.Equal("invalidPaymentOptions", (await none.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var unavailable = await host.Client.PutAsJsonAsync("/api/payment-options", new
        {
            payLaterEnabled = true,
            onlinePaymentEnabled = true,
            revision
        });
        Assert.Equal(HttpStatusCode.Conflict, unavailable.StatusCode);
        Assert.Equal("onlinePaymentNotConfigured", (await unavailable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var savedResponse = await host.Client.PutAsJsonAsync("/api/payment-options", new
        {
            payLaterEnabled = true,
            onlinePaymentEnabled = false,
            revision
        });
        Assert.Equal(HttpStatusCode.OK, savedResponse.StatusCode);
        var saved = await savedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(revision, saved.GetProperty("revision").GetGuid());

        var stale = await host.Client.PutAsJsonAsync("/api/payment-options", new
        {
            payLaterEnabled = true,
            onlinePaymentEnabled = false,
            revision
        });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("concurrency", (await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
}
