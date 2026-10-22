using HDWebFramework.Application.Checkout.CorrectOrderStatus;
using Microsoft.AspNetCore.Mvc;

namespace HDWebFramework.Api.Checkout;

public static class OrderCorrectionEndpoints
{
    public static IEndpointRouteBuilder MapOrderCorrections(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/orders/{id:guid}/correction", CorrectAsync);
        return endpoints;
    }

    public sealed record Request(Guid Revision, string Action);
    public static async Task<IResult> CorrectAsync(Guid id, Request request,
        [FromServices] CorrectOrderStatus useCase, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || request.Action is not ("payment" or "shipping") || request.Revision == Guid.Empty)
            return Results.BadRequest(new { code = "invalidCorrection" });
        var result = await useCase.ExecuteAsync(id, request.Revision,
            request.Action == "payment" ? OrderCorrection.Payment : OrderCorrection.Shipping,
            cancellationToken);
        return result switch
        {
            CorrectionResult.Success => Results.NoContent(),
            CorrectionResult.NotFound => Results.NotFound(),
            CorrectionResult.ConcurrencyConflict => Results.Conflict(new { code = "concurrencyConflict" }),
            _ => Results.Conflict(new { code = "invalidTransition", message = "Deze registratie kan niet worden teruggedraaid." }),
        };
    }
}
