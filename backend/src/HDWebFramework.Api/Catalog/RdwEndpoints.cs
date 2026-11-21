using HDWebFramework.Application.Catalog.ImportRdw;
using HDWebFramework.Application.Catalog.Rdw;
using HDWebFramework.Application.Catalog.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HDWebFramework.Api.Catalog;

public static class RdwEndpoints
{
    public static void MapRdw(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/rdw-settings", async ([FromServices] IRdwStore store, CancellationToken ct) =>
            Results.Ok(new { columns = RdwColumns.All, settings = await store.SettingsAsync(ct) }));
        endpoints.MapPut("/api/rdw-settings", async (RdwSettings request, [FromServices] IRdwStore store, CancellationToken ct) =>
        {
            try
            {
                return await store.SaveSettingsAsync(request, ct) ? Results.NoContent() :
                    (IResult)Results.Conflict(new { message = "RDW-instellingen zijn gewijzigd. Haal de gegevens opnieuw op." });
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });
        endpoints.MapPost("/api/products/{productId:guid}/rdw/{attributeId:guid}",
            async (Guid productId, Guid attributeId, RdwImportRequest request, [FromServices] ImportRdw useCase, CancellationToken ct) =>
        {
            try
            {
                return await useCase.ExecuteAsync(productId, attributeId, request.LicensePlate ?? "", ct)
                    ? Results.NoContent()
                    : (IResult)Results.NotFound(new { message = "Geen voertuig gevonden bij dit kenteken." });
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ProductConcurrencyException)
            { return Results.Conflict(new { message = "Het product is gewijzigd. Vernieuw de gegevens en probeer opnieuw." }); }
            catch (System.Text.Json.JsonException)
            { return Results.Json(new { code = "rdw", message = "RDW geeft een ongeldig antwoord. Probeer later opnieuw." }, statusCode: 502); }
            catch (HttpRequestException ex)
            {
                var reason = ex.StatusCode is { } status ? "HTTP " + (int)status : ex.HttpRequestError.ToString();
                return Results.Json(new { code = "rdw", message = "De verbinding met RDW is mislukt (" + reason + "). De bestaande voertuiggegevens zijn behouden." }, statusCode: 502);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            { return Results.Json(new { code = "rdw", message = "RDW reageert te langzaam. Probeer later opnieuw. De bestaande voertuiggegevens zijn behouden." }, statusCode: 504); }
        });
        endpoints.MapGet("/api/shop/products/{productId:guid}/rdw",
            async (Guid productId, [FromServices] IRdwStore store, CancellationToken ct) =>
        {
            var vehicles = await store.PublicVehiclesAsync(productId, ct);
            return vehicles is null ? Results.NotFound() : (IResult)Results.Ok(vehicles);
        }).AllowAnonymous();
    }
}
public sealed record RdwImportRequest(string? LicensePlate);
