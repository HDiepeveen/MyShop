using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Infrastructure.Payments;

internal sealed class MolliePaymentConfiguration
{
    public string? ApiKey { get; }
    public Uri? ReturnUrl { get; }
    public Uri? WebhookUrl { get; }
    public bool IsConfigured => ApiKey is not null && ReturnUrl is not null && WebhookUrl is not null;

    public MolliePaymentConfiguration(IConfiguration configuration)
    {
        // This integration deliberately accepts test credentials only. Live activation is a separate decision.
        var key = configuration["Payments:Mollie:ApiKey"]?.Trim();
        ApiKey = key is not null && Regex.IsMatch(key, "^test_[A-Za-z0-9]{20,64}$", RegexOptions.CultureInvariant) ? key : null;
        ReturnUrl = ParseUrl(configuration["Payments:Mollie:ReturnUrl"], true);
        WebhookUrl = ParseUrl(configuration["Payments:Mollie:WebhookUrl"], false);
    }

    private static Uri? ParseUrl(string? value, bool allowLocalhost)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.UserInfo.Length != 0
            || uri.Query.Length != 0 || uri.Fragment.Length != 0) return null;
        if (uri.IsLoopback) return allowLocalhost && uri.Scheme is "http" or "https" ? uri : null;
        return uri.Scheme == "https" ? uri : null;
    }
}

internal sealed class MollieOnlinePaymentProvider(HttpClient http, MolliePaymentConfiguration configuration)
    : IOnlinePaymentProvider, IOnlinePaymentStatusReader
{
    private static bool ValidPaymentId(string? id) => id is not null
        && Regex.IsMatch(id, "^tr_[A-Za-z0-9]{1,100}$", RegexOptions.CultureInvariant);

    public async Task<OnlinePaymentProviderStart> StartAsync(OnlinePaymentProviderRequest request, CancellationToken cancellationToken)
    {
        if (!configuration.IsConfigured || request.ProviderName != "Mollie") throw new OnlinePaymentProviderException();
        if (request.CheckoutToken == Guid.Empty || request.Totals.Count != 1
            || request.Totals[0].Currency != "EUR" || request.Totals[0].Amount <= 0)
            throw new ArgumentException("Mollie requires one positive EUR total.");
        var token = request.CheckoutToken.ToString("D");
        using var message = CreateMessage(HttpMethod.Post, "payments");
        message.Headers.Add("Idempotency-Key", request.CheckoutToken.ToString("N"));
        message.Content = JsonContent.Create(new
        {
            amount = new { currency = "EUR", value = request.Totals[0].Amount.ToString("0.00", CultureInfo.InvariantCulture) },
            description = request.PaymentReference,
            redirectUrl = configuration.ReturnUrl!.AbsoluteUri + "?checkoutToken=" + token,
            webhookUrl = configuration.WebhookUrl!.AbsoluteUri + "?checkoutToken=" + token,
            metadata = new { checkoutToken = token, paymentReference = request.PaymentReference }
        });
        using var response = await SendAsync(message, cancellationToken);
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = document.RootElement;
            var id = root.GetProperty("id").GetString();
            var url = root.GetProperty("_links").GetProperty("checkout").GetProperty("href").GetString();
            if (!ValidPaymentId(id) || root.GetProperty("mode").GetString() != "test"
                || !Matches(root, request.CheckoutToken, request.PaymentReference, request.Totals)
                || !Uri.TryCreate(url, UriKind.Absolute, out var checkout) || checkout.Scheme != "https"
                || checkout.UserInfo.Length != 0 || checkout.Host != "www.mollie.com")
                throw new OnlinePaymentProviderException();
            return new(id!, checkout);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new OnlinePaymentProviderException(); }
    }

    public async Task<OnlinePaymentVerification> VerifyAsync(OnlinePaymentStartRecord payment, CancellationToken cancellationToken)
    {
        if (!configuration.IsConfigured || payment.ProviderName != "Mollie" || !ValidPaymentId(payment.ProviderPaymentId))
            throw new OnlinePaymentProviderException();
        using var message = CreateMessage(HttpMethod.Get, "payments/" + payment.ProviderPaymentId);
        using var response = await SendAsync(message, cancellationToken);
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = document.RootElement;
            var matches = root.GetProperty("id").GetString() == payment.ProviderPaymentId
                && root.GetProperty("mode").GetString() == "test"
                && Matches(root, payment.CheckoutToken, payment.PaymentReference, payment.Totals);
            var status = root.GetProperty("status").GetString() switch
            {
                "paid" => OnlinePaymentStatus.Paid,
                "failed" => OnlinePaymentStatus.Failed,
                "canceled" => OnlinePaymentStatus.Canceled,
                "expired" => OnlinePaymentStatus.Expired,
                "pending" => OnlinePaymentStatus.Pending,
                "authorized" => OnlinePaymentStatus.Authorized,
                "open" => OnlinePaymentStatus.Open,
                _ => throw new OnlinePaymentProviderException()
            };
            return new(status, matches);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new OnlinePaymentProviderException(); }
    }

    private static bool Matches(JsonElement root, Guid token, string reference, IReadOnlyList<OrderTotalSnapshot> totals)
    {
        if (totals.Count != 1) return false;
        var amount = root.GetProperty("amount");
        var metadata = root.GetProperty("metadata");
        return amount.GetProperty("currency").GetString() == totals[0].Currency
            && decimal.TryParse(amount.GetProperty("value").GetString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            && value == totals[0].Amount
            && metadata.GetProperty("checkoutToken").GetString() == token.ToString("D")
            && metadata.GetProperty("paymentReference").GetString() == reference;
    }

    private HttpRequestMessage CreateMessage(HttpMethod method, string path)
    {
        var message = new HttpRequestMessage(method, "https://api.mollie.com/v2/" + path);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.ApiKey);
        return message;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var response = await http.SendAsync(message, cancellationToken);
            if (response.IsSuccessStatusCode) return response;
            response.Dispose();
            // Never include provider responses or credentials in public errors.
            throw new OnlinePaymentProviderException();
        }
        catch (HttpRequestException) { throw new OnlinePaymentProviderException(); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new OnlinePaymentProviderException(); }
    }
}
