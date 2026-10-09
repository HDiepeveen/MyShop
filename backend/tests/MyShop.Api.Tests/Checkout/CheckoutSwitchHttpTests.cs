using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Checkout;

public sealed class CheckoutSwitchHttpTests
{
    [SecuritySqlFact]
    public async Task Admin_can_disable_and_reenable_checkout_with_revision_and_server_enforcement()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PutAsJsonAsync("/api/payment-options",
            new { payLaterEnabled = true, checkoutEnabled = false, revision = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.True(settings.GetProperty("checkoutEnabled").GetBoolean());
        var revision = settings.GetProperty("revision").GetGuid();
        var disabled = new { checkoutEnabled = false, payLaterEnabled = true, onlinePaymentEnabled = false,
            payLaterInstructions = "Keep instructions", revision };
        var response = await host.Client.PutAsJsonAsync("/api/payment-options", disabled);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(saved.GetProperty("checkoutEnabled").GetBoolean());
        var publicOptions = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop/payment-options");
        Assert.False(publicOptions.GetProperty("checkoutEnabled").GetBoolean());
        Assert.Empty(publicOptions.GetProperty("items").EnumerateArray());
        var request = new { checkoutToken = Guid.NewGuid(), paymentMethod = "payLater", deliveryMethodId = Guid.NewGuid(),
            customerName = "Ada", email = "ada@example.test", addressLine = "Street 1", postalCode = "1234 AB", city = "City", countryCode = "NL",
            lines = new[] { new { productId = Guid.NewGuid(), variantId = Guid.NewGuid(), quantity = 1, expectedAmount = "10.00", expectedCurrency = "EUR" } } };
        foreach (var path in new[] { "/api/shop/orders", "/api/shop/online-payments" })
        {
            var rejected = await host.Client.PostAsJsonAsync(path, request);
            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            Assert.Equal("checkoutDisabled", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/payment-options", disabled)).StatusCode);
        response = await host.Client.PutAsJsonAsync("/api/payment-options", new { checkoutEnabled = true,
            payLaterEnabled = true, onlinePaymentEnabled = false, payLaterInstructions = "Keep instructions",
            revision = saved.GetProperty("revision").GetGuid() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        publicOptions = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop/payment-options");
        Assert.True(publicOptions.GetProperty("checkoutEnabled").GetBoolean());
        Assert.Equal("Keep instructions", Assert.Single(publicOptions.GetProperty("items").EnumerateArray()).GetProperty("instructions").GetString());
    }
}
