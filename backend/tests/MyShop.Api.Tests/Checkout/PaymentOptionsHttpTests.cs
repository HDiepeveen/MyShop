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
        Assert.Equal(new[] { "code", "instructions", "name" },
            option.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(JsonValueKind.Null, option.GetProperty("instructions").ValueKind);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/payment-options")).StatusCode);

        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.True(settings.GetProperty("payLaterEnabled").GetBoolean());
        Assert.False(settings.GetProperty("onlinePaymentEnabled").GetBoolean());
        Assert.Equal(JsonValueKind.Null, settings.GetProperty("payLaterInstructions").ValueKind);
        Assert.False(settings.GetProperty("onlinePaymentConfigured").GetBoolean());
        Assert.Equal(JsonValueKind.Null, settings.GetProperty("onlinePaymentProvider").ValueKind);
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
            payLaterInstructions = "  Betaal binnen 14 dagen.  ",
            revision
        });
        Assert.Equal(HttpStatusCode.OK, savedResponse.StatusCode);
        var saved = await savedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(revision, saved.GetProperty("revision").GetGuid());
        Assert.Equal("Betaal binnen 14 dagen.", saved.GetProperty("payLaterInstructions").GetString());
        var updatedPublic = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/payment-options");
        Assert.Equal("Betaal binnen 14 dagen.", updatedPublic.GetProperty("items")[0]
            .GetProperty("instructions").GetString());

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
public sealed class ConfiguredOnlinePaymentOptionsHttpTests
{
    [SecuritySqlFact]
    public async Task UnsupportedProviderIsNotExposedAsConfigured()
    {
        await using var host = await SecurityHost.Create(new Dictionary<string, string?>
        {
            ["Payments:Online:Provider"] = "Mollie"
        });

        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.False(settings.GetProperty("onlinePaymentConfigured").GetBoolean());
        Assert.Equal(JsonValueKind.Null, settings.GetProperty("onlinePaymentProvider").ValueKind);

        var response = await host.Client.PutAsJsonAsync("/api/payment-options", new
        {
            payLaterEnabled = true,
            onlinePaymentEnabled = true,
            revision = settings.GetProperty("revision").GetGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("onlinePaymentNotConfigured",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
    [SecuritySqlFact]
    public async Task AdministratorCanExposeConfiguredOnlinePaymentMethod()
    {
        await using var host = await SecurityHost.Create(new Dictionary<string, string?>
        {
            ["Payments:Online:Provider"] = "TestPay"
        });
        using var visitor = new HttpClient(new HttpClientHandler
        {
            UseCookies = false,
            AllowAutoRedirect = false
        }) { BaseAddress = host.Client.BaseAddress };

        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.True(settings.GetProperty("onlinePaymentConfigured").GetBoolean());
        Assert.Equal("TestPay", settings.GetProperty("onlinePaymentProvider").GetString());

        var savedResponse = await host.Client.PutAsJsonAsync("/api/payment-options", new
        {
            payLaterEnabled = true,
            onlinePaymentEnabled = true,
            revision = settings.GetProperty("revision").GetGuid()
        });
        Assert.Equal(HttpStatusCode.OK, savedResponse.StatusCode);

        var publicOptions = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/payment-options");
        var online = publicOptions.GetProperty("items").EnumerateArray()
            .Single(option => option.GetProperty("code").GetString() == "online");
        Assert.Equal("Direct online betalen", online.GetProperty("name").GetString());
        Assert.Equal("Je wordt doorgestuurd naar TestPay.", online.GetProperty("instructions").GetString());
    }
}
