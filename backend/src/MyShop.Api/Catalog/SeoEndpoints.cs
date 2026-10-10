using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Seo;
namespace MyShop.Api.Catalog;
public static class SeoEndpoints
{
    public static void MapSeo(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/product-types/{id:guid}/section-headings", async (Guid id, [FromServices] ManageSeo useCase, CancellationToken ct) =>
        {
            var headings = await useCase.GetTypeHeadingsAsync(id, ct);
            return headings is null ? (IResult)Results.NotFound() : Results.Ok(headings);
        });
        endpoints.MapPut("/api/product-types/{id:guid}/section-headings", async (Guid id, ProductTypeHeadingsRequest request, [FromServices] ManageSeo useCase, CancellationToken ct) =>
        {
            try { return Map(await useCase.SaveTypeHeadingsAsync(new(id, request.AboutHeading, request.AttributesHeading, request.Revision), ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { code = "invalidSeo", message = ex.Message }); }
        });
        endpoints.MapGet("/api/seo-settings", async ([FromServices] ManageSeo useCase, CancellationToken ct) => Results.Ok(await useCase.GetSettingsAsync(ct)));
        endpoints.MapPut("/api/seo-settings", async (ShopSeoSettings request, [FromServices] ManageSeo useCase, CancellationToken ct) =>
        {
            try { return Map(await useCase.SaveSettingsAsync(request.Heading, request.SeoTitle, request.Revision, ct, request.ShopName, request.WelcomeText, request.Introduction)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { code = "invalidSeo", message = ex.Message }); }
        });
        endpoints.MapGet("/api/products/{id:guid}/seo", async (Guid id, [FromServices] ManageSeo useCase, CancellationToken ct) =>
        {
            var product = await useCase.GetProductAsync(id, ct);
            return product is null ? (IResult)Results.NotFound() : Results.Ok(product);
        });
        endpoints.MapPut("/api/products/{id:guid}/seo", async (Guid id, ProductSeoRequest request, [FromServices] ManageSeo useCase, CancellationToken ct) =>
        {
            try { return Map(await useCase.SaveProductAsync(id, new(request.SeoTitle, request.SeoDescription, request.WebAddress), request.Revision, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { code = "invalidSeo", message = ex.Message }); }
        });
    }
    private static IResult Map(SeoResult result) => result.Failure switch
    {
        null => Results.NoContent(),
        SeoFailure.NotFound => Results.NotFound(),
        SeoFailure.Conflict => Results.Conflict(new { code = "concurrency", message = "De SEO-gegevens zijn gewijzigd. Vernieuw de gegevens." }),
        SeoFailure.AddressInUse => Results.Conflict(new { code = "webAddressInUse", message = "Dit webadres is al gebruikt voor een ander product. Kies een ander adres." }),
        _ => throw new InvalidOperationException()
    };
}
public sealed record ProductSeoRequest(string? SeoTitle, string? SeoDescription, string? WebAddress, Guid Revision);
public sealed record ProductTypeHeadingsRequest(string? AboutHeading, string? AttributesHeading, Guid Revision);
