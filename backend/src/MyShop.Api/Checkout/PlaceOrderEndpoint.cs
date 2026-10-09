using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Security;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.PlaceOrder;

namespace MyShop.Api.Checkout;

public static class PlaceOrderEndpoint
{
    public static void MapPlaceOrder(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/api/shop/orders", ExecuteAsync).AllowAnonymous()
            .RequireRateLimiting("storefront-order");

    private static async Task<IResult> ExecuteAsync(PlaceOrderRequest? request,
        HttpContext context, UserManager<IdentityUser> users,
        [FromServices] PlaceOrder useCase, CancellationToken cancellationToken)
    {
        if (request?.Lines is null || request.Lines.Count is < 1 or > 20)
            return Results.BadRequest(new { code = "invalidOrder" });
        try
        {
            var lines = new List<PlaceOrderLine>();
            foreach (var line in request.Lines)
            {
                if (line is null || !decimal.TryParse(line.ExpectedAmount, NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var expectedAmount))
                    return Results.BadRequest(new { code = "invalidOrder" });
                lines.Add(new(line.ProductId, line.VariantId, line.Quantity,
                    expectedAmount, line.ExpectedCurrency));
            }
            var result = await useCase.ExecuteAsync(new(request.CheckoutToken, request.PaymentMethod,
                request.CustomerName, request.Email, request.AddressLine, request.PostalCode,
                request.City, request.CountryCode, lines,
                context.User.IsInRole(AdminSecurity.CustomerRole) ? users.GetUserId(context.User) : null,
                request.DeliveryMethodId),
                cancellationToken);
            return result.Failure switch
            {
                PlaceOrderFailure.CheckoutDisabled => Results.Conflict(new
                {
                    code = "checkoutDisabled",
                    message = "Bestellen is momenteel uitgeschakeld. Je kunt het assortiment bekijken."
                }),
                PlaceOrderFailure.CartUnavailable => Results.Conflict(new
                {
                    code = "cartChanged",
                    message = "De winkelmand is gewijzigd. Controleer de artikelen en prijzen opnieuw."
                }),
                PlaceOrderFailure.PaymentUnavailable => Results.Conflict(new
                {
                    code = "paymentUnavailable",
                    message = "De gekozen betaaloptie is niet meer beschikbaar."
                }),
                PlaceOrderFailure.OnlinePaymentRequired => Results.Conflict(new
                {
                    code = "onlinePaymentRequired",
                    message = "Start eerst de online betaling."
                }),
                PlaceOrderFailure.DeliveryUnavailable => Results.Conflict(new
                {
                    code = "deliveryUnavailable",
                    message = "De gekozen bezorgoptie is niet meer beschikbaar."
                }),
                null => Results.Ok(new PlaceOrderResponse(result.Receipt!.Id, result.Receipt.Number,
                    result.Receipt.PlacedAt, result.Receipt.PaymentInstructions,
                    result.Receipt.Totals.Select(MapTotal).ToArray(),
                    result.Receipt.DeliveryMethod is null ? null : MapDelivery(result.Receipt.DeliveryMethod))),
                _ => throw new InvalidOperationException()
            };
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new
            {
                code = "invalidOrder",
                message = "Controleer de klantgegevens, het adres en de winkelmand."
            });
        }
    }

    private static PlaceOrderTotalResponse MapTotal(OrderTotalSnapshot total) =>
        new(total.Currency, total.Amount.ToString("0.00", CultureInfo.InvariantCulture));

    private static PlaceOrderDeliveryMethodResponse MapDelivery(OrderDeliveryMethodSnapshot delivery) =>
        new(delivery.Id, delivery.Name, delivery.Description,
            delivery.Amount.ToString("0.00", CultureInfo.InvariantCulture), delivery.Currency);
}

public sealed record PlaceOrderRequest(Guid CheckoutToken, string PaymentMethod, string CustomerName,
    string Email, string AddressLine, string PostalCode, string City, string CountryCode,
    Guid DeliveryMethodId, IReadOnlyList<PlaceOrderLineRequest?> Lines);
public sealed record PlaceOrderLineRequest(Guid ProductId, Guid VariantId, int Quantity,
    string ExpectedAmount, string ExpectedCurrency);
public sealed record PlaceOrderResponse(Guid Id, string Number, DateTimeOffset PlacedAt,
    string? PaymentInstructions, IReadOnlyList<PlaceOrderTotalResponse> Totals,
    PlaceOrderDeliveryMethodResponse? DeliveryMethod);
public sealed record PlaceOrderTotalResponse(string Currency, string Amount);
public sealed record PlaceOrderDeliveryMethodResponse(Guid Id, string Name, string? Description,
    string Amount, string Currency);
