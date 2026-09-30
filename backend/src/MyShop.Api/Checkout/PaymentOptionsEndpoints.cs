using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Checkout.GetAdminPaymentOptions;
using MyShop.Application.Checkout.GetPaymentOptions;
using MyShop.Application.Checkout.UpdatePaymentOptions;

namespace MyShop.Api.Checkout;

public static class PaymentOptionsEndpoints
{
    public static void MapPaymentOptions(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/shop/payment-options", async ([FromServices] GetPaymentOptions useCase,
            CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(cancellationToken)))
            .AllowAnonymous();
        endpoints.MapGet("/api/payment-options", async ([FromServices] GetAdminPaymentOptions useCase,
            CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(cancellationToken)));
        endpoints.MapPut("/api/payment-options", Update);
    }

    private static async Task<IResult> Update(UpdatePaymentOptionsRequest request,
        [FromServices] UpdatePaymentOptions useCase, CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCase.ExecuteAsync(new(request.PayLaterEnabled,
                request.OnlinePaymentEnabled, request.PayLaterInstructions, request.Revision), cancellationToken);
            return result.Failure switch
            {
                UpdatePaymentOptionsFailure.OnlinePaymentNotConfigured => Results.Conflict(new
                {
                    code = "onlinePaymentNotConfigured",
                    message = "Koppel eerst een online betaalprovider."
                }),
                UpdatePaymentOptionsFailure.ConcurrencyConflict => Results.Conflict(new
                {
                    code = "concurrency",
                    message = "De betaalinstellingen zijn intussen gewijzigd."
                }),
                null => Results.Ok(result.Settings),
                _ => throw new InvalidOperationException()
            };
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { code = "invalidPaymentOptions", message = exception.Message });
        }
    }
}

public sealed record UpdatePaymentOptionsRequest(bool PayLaterEnabled, bool OnlinePaymentEnabled,
    string? PayLaterInstructions, Guid Revision);
