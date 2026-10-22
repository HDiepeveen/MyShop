using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Checkout.MollieSettings;
namespace HDWebFramework.Api.Checkout;
public static class MollieSettingsEndpoints
{
    public static void MapMollieSettings(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/payment-options/mollie", async ([FromServices] ManageMollieSettings useCase, CancellationToken ct) => Results.Ok(await useCase.GetAsync(ct)));
        endpoints.MapPut("/api/payment-options/mollie", async (UpdateMollieSettingsCommand request, [FromServices] ManageMollieSettings useCase, [FromServices] IWebHostEnvironment environment, CancellationToken ct) =>
        {
            try
            {
                var settings = await useCase.SaveAsync(request, environment.IsDevelopment(), ct);
                return settings is null ? (IResult)Results.Conflict(new { code = "concurrency" }) : Results.Ok(settings);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { code = "invalidMollieSettings", message = ex.Message }); }
        });
        endpoints.MapPost("/api/payment-options/mollie/check", async (MollieCheckRequest request, [FromServices] ManageMollieSettings useCase, CancellationToken ct) =>
        {
            var result = await useCase.CheckAsync(request.Revision, ct);
            return result is null ? (IResult)Results.Conflict(new { code = "concurrency" }) : Results.Ok(result);
        });
    }
}
public sealed record MollieCheckRequest(Guid Revision);
