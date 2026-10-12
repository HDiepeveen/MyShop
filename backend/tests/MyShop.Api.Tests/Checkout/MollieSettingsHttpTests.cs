using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;
namespace MyShop.Api.Tests.Checkout;
public sealed class MollieSettingsHttpTests
{
    [SecuritySqlFact]
    public async Task Settings_validate_test_keys_urls_revisions_and_never_return_the_secret()
    {
        await using var host = await SecurityHost.Create();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/payment-options/mollie")).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();
        var first = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options/mollie");
        var revision = first.GetProperty("revision").GetGuid();
        const string returnUrl = "http://localhost:4200/winkel/betaling";
        const string webhookUrl = "https://example.test/api/payments/mollie/webhook";
        const string key = "test_abcdefghijklmnopqrstuvwxyz";
        foreach (var invalid in new[] {
            new { returnUrl, webhookUrl, apiKey = "live_abcdefghijklmnopqrstuvwxyz", clearKey = false, revision },
            new { returnUrl, webhookUrl = "http://example.test/api/payments/mollie/webhook", apiKey = key, clearKey = false, revision },
            new { returnUrl, webhookUrl = "https://localhost/api/payments/mollie/webhook", apiKey = key, clearKey = false, revision },
            new { returnUrl = "https://example.test/incorrect", webhookUrl, apiKey = key, clearKey = false, revision },
            new { returnUrl, webhookUrl, apiKey = key, clearKey = true, revision }
        })
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/payment-options/mollie", invalid)).StatusCode);
        var saved = await host.Client.PutAsJsonAsync("/api/payment-options/mollie", new { returnUrl, webhookUrl, apiKey = key, clearKey = false, revision });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.DoesNotContain(key, await saved.Content.ReadAsStringAsync());
        var snapshot = await saved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "keyConfigured", "managed", "returnUrl", "revision", "webhookUrl" }, snapshot.EnumerateObject().Select(x => x.Name).Order());
        Assert.True(snapshot.GetProperty("managed").GetBoolean()); Assert.True(snapshot.GetProperty("keyConfigured").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/payment-options/mollie", new { returnUrl, webhookUrl, apiKey = key, clearKey = false, revision })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsJsonAsync("/api/payment-options/mollie/check", new { revision })).StatusCode);
        var options = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/payment-options", new { payLaterEnabled = true, onlinePaymentEnabled = true, revision = options.GetProperty("revision").GetGuid() })).StatusCode);
        revision = snapshot.GetProperty("revision").GetGuid();
        var kept = await host.Client.PutAsJsonAsync("/api/payment-options/mollie", new { returnUrl, webhookUrl, apiKey = (string?)null, clearKey = false, revision });
        Assert.True((await kept.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("keyConfigured").GetBoolean());
        revision = (await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options/mollie")).GetProperty("revision").GetGuid();
        var cleared = await host.Client.PutAsJsonAsync("/api/payment-options/mollie", new { returnUrl, webhookUrl, apiKey = (string?)null, clearKey = true, revision });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        snapshot = await cleared.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(snapshot.GetProperty("keyConfigured").GetBoolean());
        var check = await host.Client.PostAsJsonAsync("/api/payment-options/mollie/check", new { revision = snapshot.GetProperty("revision").GetGuid() });
        Assert.False((await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("configured").GetBoolean());
        options = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.False(options.GetProperty("onlinePaymentConfigured").GetBoolean());
        Assert.False(options.GetProperty("onlinePaymentEnabled").GetBoolean());
    }
}
