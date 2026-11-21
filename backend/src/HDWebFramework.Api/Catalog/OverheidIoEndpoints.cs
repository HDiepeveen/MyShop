using HDWebFramework.Application.Catalog.ImportOverheidIo;
using HDWebFramework.Application.Catalog.OverheidIo;
using HDWebFramework.Application.Catalog.Abstractions;
using Microsoft.AspNetCore.Mvc;
namespace HDWebFramework.Api.Catalog;
public static class OverheidIoEndpoints
{
    public static void MapOverheidIo(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/overheid-io-settings", async ([FromServices] IOverheidIoSettingsStore store, CancellationToken ct) =>
            Results.Ok(new { columns = OverheidIoColumns.All, settings = await store.GetAsync(ct) }));
        endpoints.MapPut("/api/overheid-io-settings", async (SaveOverheidIoSettings request, [FromServices] IOverheidIoSettingsStore store, CancellationToken ct) =>
        {
            try { return await store.SaveAsync(request, ct) ? Results.NoContent() : (IResult)Results.Conflict(new { message = "Voertuiginstellingen zijn gewijzigd. Vernieuw de gegevens." }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });
        endpoints.MapPost("/api/products/{productId:guid}/overheid-io/{attributeId:guid}",
            async (Guid productId, Guid attributeId, OverheidIoImportRequest request, [FromServices] ImportOverheidIo useCase, CancellationToken ct) =>
        {
            try
            {
                return await useCase.ExecuteAsync(productId, attributeId, request.LicensePlate ?? "", ct)
                    ? Results.NoContent()
                    : (IResult)Results.NotFound(new { message = "Geen voertuig gevonden bij dit kenteken." });
            }
            catch (OverheidIoConfigurationException ex)
            { return Results.Json(new { code = "overheidIo", message = ex.Message }, statusCode: 422); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ProductConcurrencyException)
            { return Results.Conflict(new { message = "Het product is gewijzigd. Vernieuw de gegevens en probeer opnieuw." }); }
            catch (System.Text.Json.JsonException)
            { return Results.Json(new { code = "overheidIo", message = "Overheid.io geeft een ongeldig antwoord. Probeer later opnieuw." }, statusCode: 502); }
            catch (HttpRequestException ex)
            {
                var reason = ex.StatusCode is { } status ? "HTTP " + (int)status : ex.HttpRequestError.ToString();
                return Results.Json(new { code = "overheidIo", message = "De verbinding met Overheid.io is mislukt (" + reason + "). De bestaande voertuiggegevens zijn behouden." }, statusCode: 502);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            { return Results.Json(new { code = "overheidIo", message = "Overheid.io reageert te langzaam. Probeer later opnieuw. De bestaande voertuiggegevens zijn behouden." }, statusCode: 504); }
        });
    }
}
public sealed record OverheidIoImportRequest(string? LicensePlate);
