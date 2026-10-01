using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.ManageDeliveryMethods;

namespace MyShop.Api.Checkout;

public static class DeliveryMethodEndpoints
{
    public static void MapDeliveryMethods(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/shop/delivery-methods", async (
            [FromServices] ListDeliveryMethods useCase, CancellationToken cancellationToken) =>
            Results.Ok((await useCase.ExecuteAsync(true, cancellationToken)).Select(PublicResponse)))
            .AllowAnonymous();
        endpoints.MapGet("/api/delivery-methods", async (
            [FromServices] ListDeliveryMethods useCase, CancellationToken cancellationToken) =>
            Results.Ok((await useCase.ExecuteAsync(false, cancellationToken)).Select(Response)));
        endpoints.MapPost("/api/delivery-methods", CreateAsync);
        endpoints.MapPut("/api/delivery-methods/{id:guid}", UpdateAsync);
        endpoints.MapDelete("/api/delivery-methods/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> CreateAsync(DeliveryMethodRequest? request,
        [FromServices] SaveDeliveryMethod useCase, CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest();
        if (!TryAmount(request.Amount, out var amount)) return Invalid();
        try
        {
            var result = await useCase.ExecuteAsync(new(null, request.Name, request.Description,
                amount, request.Currency, request.Enabled, null), cancellationToken);
            return Results.Created($"/api/delivery-methods/{result.Method!.Id}", Response(result.Method));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    private static async Task<IResult> UpdateAsync(Guid id, DeliveryMethodRequest? request,
        [FromServices] SaveDeliveryMethod useCase, CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest();
        if (!TryAmount(request.Amount, out var amount)) return Invalid();
        try
        {
            var result = await useCase.ExecuteAsync(new(id, request.Name, request.Description,
                amount, request.Currency, request.Enabled, request.Revision), cancellationToken);
            return result.Failure switch
            {
                SaveDeliveryMethodFailure.NotFound => Results.NotFound(),
                SaveDeliveryMethodFailure.ConcurrencyConflict => Conflict(),
                null => Results.Ok(Response(result.Method!)),
                _ => throw new InvalidOperationException()
            };
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    private static async Task<IResult> DeleteAsync(Guid id, Guid revision,
        [FromServices] DeleteDeliveryMethod useCase, CancellationToken cancellationToken)
    {
        try
        {
            var failure = await useCase.ExecuteAsync(id, revision, cancellationToken);
            return failure switch
            {
                DeleteDeliveryMethodFailure.NotFound => Results.NotFound(),
                DeleteDeliveryMethodFailure.ConcurrencyConflict => Conflict(),
                null => Results.NoContent(),
                _ => throw new InvalidOperationException()
            };
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    private static bool TryAmount(string value, out decimal amount) =>
        decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
    private static IResult Invalid(string message = "Controleer de bezorgoptie.") =>
        Results.BadRequest(new { code = "invalidDeliveryMethod", message });
    private static IResult Conflict() => Results.Conflict(new
    { code = "concurrency", message = "De bezorgoptie is intussen gewijzigd." });
    private static DeliveryMethodResponse Response(DeliveryMethodSnapshot method) =>
        new(method.Id, method.Name, method.Description,
            method.Amount.ToString("0.00", CultureInfo.InvariantCulture), method.Currency,
            method.Enabled, method.Revision);
    private static PublicDeliveryMethodResponse PublicResponse(DeliveryMethodSnapshot method) =>
        new(method.Id, method.Name, method.Description,
            method.Amount.ToString("0.00", CultureInfo.InvariantCulture), method.Currency);
}

public sealed record DeliveryMethodRequest(string Name, string? Description, string Amount,
    string Currency, bool Enabled, Guid? Revision);
public sealed record DeliveryMethodResponse(Guid Id, string Name, string? Description,
    string Amount, string Currency, bool Enabled, Guid Revision);
public sealed record PublicDeliveryMethodResponse(Guid Id, string Name, string? Description,
    string Amount, string Currency);
