using Microsoft.AspNetCore.Http.Features;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.CompleteOnlinePayment;

namespace MyShop.Api.Checkout;

// Only this provider callback is exempt from browser CSRF protection.
public sealed class PaymentWebhookMetadata { }

public static class MollieWebhookEndpoint
{
    public static void MapMollieWebhook(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/api/payments/mollie/webhook", ExecuteAsync)
            .AllowAnonymous().WithMetadata(new PaymentWebhookMetadata())
            .RequireRateLimiting("payment-webhook");

    private static async Task<IResult> ExecuteAsync(HttpContext context, Guid checkoutToken,
        IOnlinePaymentStartRepository starts, CompleteOnlinePayment complete, CancellationToken cancellationToken)
    {
        if (checkoutToken == Guid.Empty || !context.Request.HasFormContentType || context.Request.ContentLength > 4096)
            return Results.BadRequest();
        var size = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (size is { IsReadOnly: false }) size.MaxRequestBodySize = 4096;
        IFormCollection form;
        try { form = await context.Request.ReadFormAsync(cancellationToken); }
        catch (InvalidDataException) { return Results.BadRequest(); }
        catch (BadHttpRequestException) { return Results.BadRequest(); }
        var ids = form["id"];
        if (ids.Count != 1 || string.IsNullOrWhiteSpace(ids[0]) || ids[0]!.Length > 103) return Results.BadRequest();
        var payment = await starts.GetByCheckoutTokenAsync(checkoutToken, cancellationToken);
        if (payment is null) return Results.StatusCode(503); // Mollie may call before the start has been persisted; retry.
        if (payment.ProviderName != "Mollie" || payment.ProviderPaymentId != ids[0]) return Results.BadRequest();
        try
        {
            var result = await complete.ExecuteAsync(new(checkoutToken, ids[0]), cancellationToken);
            return result.Failure switch
            {
                CompleteOnlinePaymentFailure.PaymentMismatch => Results.BadRequest(),
                CompleteOnlinePaymentFailure.CartUnavailable => Results.StatusCode(503),
                CompleteOnlinePaymentFailure.PaymentNotFound => Results.StatusCode(503),
                _ => Results.Ok()
            };
        }
        catch (OnlinePaymentProviderException) { return Results.StatusCode(503); }
    }
}
