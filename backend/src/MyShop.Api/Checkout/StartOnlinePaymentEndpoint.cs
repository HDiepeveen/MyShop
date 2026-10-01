using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Checkout.StartOnlinePayment;
using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Api.Checkout;

public static class StartOnlinePaymentEndpoint
{
    public static void MapStartOnlinePayment(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/api/shop/online-payments", ExecuteAsync).AllowAnonymous()
            .RequireRateLimiting("storefront-order");

    private static async Task<IResult> ExecuteAsync(StartOnlinePaymentRequest? request,
        [FromServices] StartOnlinePayment useCase, CancellationToken cancellationToken)
    {
        if (request?.Lines is null || request.Lines.Count is < 1 or > 20)
            return Results.BadRequest(new { code = "invalidPaymentStart" });
        try
        {
            var lines = new List<StartOnlinePaymentLine>();
            foreach (var line in request.Lines)
            {
                if (line is null || !decimal.TryParse(line.ExpectedAmount, NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var expectedAmount))
                    return Results.BadRequest(new { code = "invalidPaymentStart" });
                lines.Add(new(line.ProductId, line.VariantId, line.Quantity,
                    expectedAmount, line.ExpectedCurrency));
            }
            var result = await useCase.ExecuteAsync(new(request.CheckoutToken, request.DeliveryMethodId, lines),
                cancellationToken);
            return result.Failure switch
            {
                StartOnlinePaymentFailure.CartUnavailable => Results.Conflict(new
                {
                    code = "cartChanged",
                    message = "De winkelmand is gewijzigd. Controleer de artikelen en prijzen opnieuw."
                }),
                StartOnlinePaymentFailure.PaymentUnavailable => Results.Conflict(new
                {
                    code = "paymentUnavailable",
                    message = "De gekozen betaaloptie is niet meer beschikbaar."
                }),
                StartOnlinePaymentFailure.DeliveryUnavailable => Results.Conflict(new
                {
                    code = "deliveryUnavailable",
                    message = "De gekozen bezorgoptie is niet meer beschikbaar."
                }),
                null => Results.Ok(new StartOnlinePaymentResponse(result.Payment!.CheckoutToken,
                    result.Payment.ProviderName,
                    "De online betaalprovider is klaar om gekoppeld te worden.",
                    result.Payment.Totals.Select(MapTotal).ToArray(), MapDelivery(result.Payment.DeliveryMethod))),
                _ => throw new InvalidOperationException()
            };
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new
            {
                code = "invalidPaymentStart",
                message = "Controleer de winkelmand en bezorgkeuze."
            });
        }
    }

    private static StartOnlinePaymentTotalResponse MapTotal(OrderTotalSnapshot total) =>
        new(total.Currency, total.Amount.ToString("0.00", CultureInfo.InvariantCulture));

    private static StartOnlinePaymentDeliveryMethodResponse MapDelivery(OrderDeliveryMethodSnapshot delivery) =>
        new(delivery.Id, delivery.Name, delivery.Description,
            delivery.Amount.ToString("0.00", CultureInfo.InvariantCulture), delivery.Currency);
}

public sealed record StartOnlinePaymentRequest(Guid CheckoutToken, Guid DeliveryMethodId,
    IReadOnlyList<StartOnlinePaymentLineRequest?> Lines);
public sealed record StartOnlinePaymentLineRequest(Guid ProductId, Guid VariantId, int Quantity,
    string ExpectedAmount, string ExpectedCurrency);
public sealed record StartOnlinePaymentResponse(Guid CheckoutToken, string ProviderName, string Message,
    IReadOnlyList<StartOnlinePaymentTotalResponse> Totals,
    StartOnlinePaymentDeliveryMethodResponse DeliveryMethod);
public sealed record StartOnlinePaymentTotalResponse(string Currency, string Amount);
public sealed record StartOnlinePaymentDeliveryMethodResponse(Guid Id, string Name, string? Description,
    string Amount, string Currency);
