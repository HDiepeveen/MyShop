using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetStorefrontPrices;
using MyShop.Application.Catalog.BrowseStorefront;
using MyShop.Application.Catalog.GetStorefrontProduct;
using MyShop.Domain.Catalog;

namespace MyShop.Api.Storefront;

public static class StorefrontEndpoints
{
    public static void MapStorefront(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCartQuote();
        endpoints.MapGet("/api/shop/products/{productId}/prices", async (string productId,
            [FromServices] GetStorefrontPrices useCase, CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty) return Results.NotFound();
            var prices = await useCase.ExecuteAsync(new(ProductId.From(id), DateTimeOffset.UtcNow), cancellationToken);
            return prices is null ? (IResult)Results.NotFound() : Results.Ok(prices);
        }).AllowAnonymous();
        endpoints.MapGet("/api/shop/products", async (int? offset, int? limit, string? search,
            [FromServices] BrowseStorefront useCase, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await useCase.ExecuteAsync(new(offset ?? 0, limit ?? 20, search), cancellationToken)); }
            catch (ArgumentException) { return (IResult)Results.BadRequest(); }
        }).AllowAnonymous();
        endpoints.MapGet("/api/shop/products/{productId}", async (string productId,
            [FromServices] GetStorefrontProduct useCase, CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty) return Results.NotFound();
            var product = await useCase.ExecuteAsync(new(ProductId.From(id)), cancellationToken);
            return product is null ? (IResult)Results.NotFound() : Results.Ok(product);
        }).AllowAnonymous();
    }
}
