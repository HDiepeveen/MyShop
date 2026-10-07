using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.CompleteOnlinePayment;

namespace MyShop.Api.Checkout;

public static class CompleteOnlinePaymentEndpoint
{
    public static void MapCompleteOnlinePayment(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/shop/online-payments/{checkoutToken:guid}/complete", ExecuteAsync)
            .AllowAnonymous()
            .RequireRateLimiting("storefront-order");

    private static async Task<IResult> ExecuteAsync(Guid checkoutToken, string? providerPaymentId,
        [FromServices] CompleteOnlinePayment useCase, CancellationToken cancellationToken)
    {
        if (checkoutToken == Guid.Empty)
            return Results.BadRequest(new { code = "invalidPayment" });
        try
        {
            var result = await useCase.ExecuteAsync(new(checkoutToken, providerPaymentId), cancellationToken);
            return result.Failure switch
            {
                CompleteOnlinePaymentFailure.PaymentNotFound => Results.NotFound(new
                {
                    code = "paymentNotFound",
                    message = "De betaling is niet gevonden. Start de online betaling opnieuw."
                }),
                CompleteOnlinePaymentFailure.PaymentMismatch => Results.Conflict(new
                {
                    code = "paymentMismatch",
                    message = "De betaling hoort niet bij deze checkout. Start de online betaling opnieuw."
                }),
                CompleteOnlinePaymentFailure.PaymentPending => Results.Conflict(new { code = "paymentPending", message = "De betaling is nog niet bevestigd. Controleer de betaalstatus opnieuw." }),
                CompleteOnlinePaymentFailure.PaymentFailed => Results.Conflict(new { code = "paymentFailed", message = "De betaling is mislukt. Je winkelmand is bewaard; je kunt opnieuw afrekenen." }),
                CompleteOnlinePaymentFailure.PaymentCanceled => Results.Conflict(new { code = "paymentCanceled", message = "De betaling is geannuleerd. Je winkelmand is bewaard; je kunt opnieuw afrekenen." }),
                CompleteOnlinePaymentFailure.PaymentExpired => Results.Conflict(new { code = "paymentExpired", message = "De betaling is verlopen. Je winkelmand is bewaard; je kunt opnieuw afrekenen." }),
                CompleteOnlinePaymentFailure.CartUnavailable => Results.Conflict(new
                {
                    code = "cartUnavailable",
                    message = "De winkelmand is niet meer beschikbaar. Neem contact op met de webshop."
                }),
                null => Results.Ok(new CompleteOnlinePaymentResponse(result.Receipt!.Id,
                    result.Receipt.Number, result.Receipt.PlacedAt, result.Receipt.Totals.Select(MapTotal).ToArray(),
                    result.Receipt.DeliveryMethod is null ? null : MapDelivery(result.Receipt.DeliveryMethod))),
                _ => throw new InvalidOperationException()
            };
        }
        catch (OnlinePaymentProviderException)
        {
            return Results.Json(new { code = "providerUnavailable", message = "De betaalstatus kon niet worden opgehaald. Controleer opnieuw; start nog geen nieuwe betaling." }, statusCode: 503);
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new { code = "invalidPayment" });
        }
    }

    private static CompleteOnlinePaymentTotalResponse MapTotal(OrderTotalSnapshot total) =>
        new(total.Currency, total.Amount.ToString("0.00", CultureInfo.InvariantCulture));

    private static CompleteOnlinePaymentDeliveryMethodResponse MapDelivery(OrderDeliveryMethodSnapshot delivery) =>
        new(delivery.Id, delivery.Name, delivery.Description,
            delivery.Amount.ToString("0.00", CultureInfo.InvariantCulture), delivery.Currency);
}

public sealed record CompleteOnlinePaymentResponse(Guid Id, string Number, DateTimeOffset PlacedAt,
    IReadOnlyList<CompleteOnlinePaymentTotalResponse> Totals,
    CompleteOnlinePaymentDeliveryMethodResponse? DeliveryMethod);
public sealed record CompleteOnlinePaymentTotalResponse(string Currency, string Amount);
public sealed record CompleteOnlinePaymentDeliveryMethodResponse(Guid Id, string Name, string? Description,
    string Amount, string Currency);
