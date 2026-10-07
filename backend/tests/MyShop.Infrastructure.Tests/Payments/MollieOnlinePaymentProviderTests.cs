using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Infrastructure.Payments;

namespace MyShop.Infrastructure.Tests.Payments;

public sealed class MollieOnlinePaymentProviderTests
{
    private static readonly Guid Token = Guid.Parse("95186104-e167-4ce3-94d9-26f982e5e10a");
    private static Dictionary<string, string?> Settings() => new()
    {
        ["Payments:Online:Provider"] = "Mollie",
        ["Payments:Mollie:ApiKey"] = "test_abcdefghijklmnopqrstuvwxyz",
        ["Payments:Mollie:ReturnUrl"] = "http://localhost:4200/winkel/betaling",
        ["Payments:Mollie:WebhookUrl"] = "https://shop.example.test/api/payments/mollie/webhook"
    };
    private static OnlinePaymentStartRecord Payment() => new(Token, "Mollie", "OP-TEST", "tr_123", new("https://www.mollie.com/checkout/test"),
        new("Ada", "ada@example.test"), new("Street 1", "1234 AB", "Utrecht", "NL"), [], [new("EUR", 29.95m)], new(Guid.NewGuid(), "Shipping", null, 4.95m, "EUR"), DateTimeOffset.UtcNow);
    private static string Response(string status = "paid", string amount = "29.95", string mode = "test", string currency = "EUR", string? token = null, string reference = "OP-TEST", string url = "https://www.mollie.com/checkout/test", string id = "tr_123") => JsonSerializer.Serialize(new
    {
        id, mode, status, amount = new { currency, value = amount },
        metadata = new { checkoutToken = token ?? Token.ToString("D"), paymentReference = reference },
        _links = new { checkout = new { href = url } }
    });
    private static MollieOnlinePaymentProvider Provider(Handler handler) => new(new HttpClient(handler), new(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build()));

    [Fact]
    public async Task CreatesTestPaymentWithExactAmountMetadataAndStableIdempotencyKey()
    {
        var handler = new Handler(Response());
        var provider = Provider(handler);
        var result = await provider.StartAsync(new("Mollie", Token, "OP-TEST", [new("EUR", 29.95m)]), CancellationToken.None);
        Assert.Equal("tr_123", result.ProviderPaymentId);
        Assert.Equal("https://api.mollie.com/v2/payments", handler.Url);
        Assert.Equal(Token.ToString("N"), handler.IdempotencyKey);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("29.95", body.RootElement.GetProperty("amount").GetProperty("value").GetString());
        Assert.Equal(Token.ToString("D"), body.RootElement.GetProperty("metadata").GetProperty("checkoutToken").GetString());
        Assert.EndsWith("?checkoutToken=" + Token, body.RootElement.GetProperty("redirectUrl").GetString());
        Assert.EndsWith("?checkoutToken=" + Token, body.RootElement.GetProperty("webhookUrl").GetString());
    }

    [Theory]
    [InlineData("open", OnlinePaymentStatus.Open)]
    [InlineData("pending", OnlinePaymentStatus.Pending)]
    [InlineData("paid", OnlinePaymentStatus.Paid)]
    [InlineData("failed", OnlinePaymentStatus.Failed)]
    [InlineData("canceled", OnlinePaymentStatus.Canceled)]
    [InlineData("expired", OnlinePaymentStatus.Expired)]
    [InlineData("authorized", OnlinePaymentStatus.Authorized)]
    public async Task ReadsAuthoritativePaymentStatus(string status, OnlinePaymentStatus expected)
    {
        var handler = new Handler(Response(status));
        var verification = await Provider(handler).VerifyAsync(Payment(), CancellationToken.None);
        Assert.Equal(expected, verification.Status); Assert.True(verification.MatchesPayment);
        Assert.Equal("https://api.mollie.com/v2/payments/tr_123", handler.Url);
        Assert.Null(handler.IdempotencyKey);
    }

    [Theory]
    [InlineData("amount")][InlineData("currency")][InlineData("token")][InlineData("reference")][InlineData("mode")][InlineData("id")]
    public async Task RejectsPaidPaymentThatDoesNotMatchStoredStart(string field)
    {
        var response = Response(amount: field == "amount" ? "1.00" : "29.95", currency: field == "currency" ? "USD" : "EUR",
            token: field == "token" ? Guid.NewGuid().ToString("D") : null, reference: field == "reference" ? "OTHER" : "OP-TEST",
            mode: field == "mode" ? "live" : "test", id: field == "id" ? "tr_other" : "tr_123");
        Assert.False((await Provider(new(response)).VerifyAsync(Payment(), CancellationToken.None)).MatchesPayment);
    }

    [Theory]
    [InlineData("https://attacker.example/checkout")][InlineData("http://www.mollie.com/checkout")][InlineData("https://www.mollie.com@attacker.example/checkout")]
    public async Task RejectsUnsafeCheckoutUrl(string url) => await Assert.ThrowsAsync<OnlinePaymentProviderException>(() =>
        Provider(new(Response(url: url))).StartAsync(new("Mollie", Token, "OP-TEST", [new("EUR", 29.95m)]), CancellationToken.None));

    [Theory]
    [InlineData("Payments:Mollie:ApiKey", "live_abcdefghijklmnopqrstuvwxyz")]
    [InlineData("Payments:Mollie:ApiKey", "")]
    [InlineData("Payments:Mollie:WebhookUrl", "http://localhost:5032/api/payments/mollie/webhook")]
    [InlineData("Payments:Mollie:WebhookUrl", "https://localhost/api/payments/mollie/webhook")]
    [InlineData("Payments:Mollie:ReturnUrl", "https://user:pass@shop.example.test/winkel/betaling")]
    [InlineData("Payments:Mollie:ReturnUrl", "https://shop.example.test/winkel/betaling?token=foo")]
    public void IncompleteOrLiveConfigurationDoesNotEnableMollie(string setting, string value)
    {
        var settings = Settings(); settings[setting] = value;
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.False(new OnlinePaymentAvailability(config).IsConfigured);
    }

    [Theory]
    [InlineData(401)][InlineData(422)][InlineData(429)][InlineData(500)]
    public async Task ProviderErrorsAreSafeAndDoNotExposeResponse(int status)
    {
        var exception = await Assert.ThrowsAsync<OnlinePaymentProviderException>(() => Provider(new("sensitive-provider-response", (HttpStatusCode)status)).VerifyAsync(Payment(), CancellationToken.None));
        Assert.DoesNotContain("sensitive", exception.ToString());
    }

    [Theory]
    [InlineData("{}")] [InlineData("not-json")] [InlineData("{\"status\":\"paid\"}")]
    public async Task MalformedResponseFailsClosed(string json) => await Assert.ThrowsAsync<OnlinePaymentProviderException>(() => Provider(new(json)).VerifyAsync(Payment(), CancellationToken.None));

    [Fact]
    public async Task MultipleCurrenciesNeverReachProvider()
    {
        var handler = new Handler(Response());
        await Assert.ThrowsAsync<ArgumentException>(() => Provider(handler).StartAsync(new("Mollie", Token, "OP-TEST", [new("EUR", 20), new("USD", 5)]), CancellationToken.None));
        Assert.Null(handler.Url);
    }

    private sealed class Handler(string response, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Url, Body, IdempotencyKey, AuthorizationScheme;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.AbsoluteUri; Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.Single() : null;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            return new(status) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
