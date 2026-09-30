using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using MyShop.Application.Catalog.GetStorefrontPrices;
using MyShop.Application.Catalog.BrowseStorefront;
using MyShop.Application.Catalog.GetStorefrontProduct;
using MyShop.Application.Catalog.ListStorefrontCategories;
using MyShop.Domain.Catalog;
using MyShop.Api.Checkout;

namespace MyShop.Api.Storefront;

public static class StorefrontEndpoints
{
    public static void MapStorefront(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCartQuote();
        endpoints.MapPlaceOrder();
        endpoints.MapGet("/api/shop/categories", async (
            [FromServices] ListStorefrontCategories useCase, CancellationToken cancellationToken) =>
            Results.Ok(await useCase.ExecuteAsync(cancellationToken))).AllowAnonymous();
        endpoints.MapGet("/api/shop/products/{productId}/prices", async (string productId,
            [FromServices] GetStorefrontPrices useCase, CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty) return Results.NotFound();
            var prices = await useCase.ExecuteAsync(new(ProductId.From(id), DateTimeOffset.UtcNow), cancellationToken);
            return prices is null ? (IResult)Results.NotFound() : Results.Ok(prices);
        }).AllowAnonymous();
        endpoints.MapGet("/api/shop/products", async (int? offset, int? limit, string? search, Guid? categoryId,
            [FromServices] BrowseStorefront useCase, CancellationToken cancellationToken) =>
        {
            try
            {
                CategoryId? category = categoryId is null ? null : CategoryId.From(categoryId.Value);
                var page = await useCase.ExecuteAsync(
                    new(offset ?? 0, limit ?? 20, search, category, DateTimeOffset.UtcNow),
                    cancellationToken);
                return Results.Ok(new StorefrontPageResponse(page.At,
                    page.Items.Select(item => new StorefrontItemResponse(item.Id, item.Name,
                        item.ImageUrl, item.ImageAlt, item.Prices.Select(price =>
                            new StorefrontPriceRangeResponse(price.Currency,
                                price.MinimumAmount.ToString("F2", CultureInfo.InvariantCulture),
                                price.MaximumAmount.ToString("F2", CultureInfo.InvariantCulture))).ToList()))
                        .ToList(), page.TotalCount, page.Offset, page.Limit));
            }
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

public sealed record StorefrontPageResponse(DateTimeOffset At, IReadOnlyList<StorefrontItemResponse> Items,
    int TotalCount, int Offset, int Limit);
public sealed record StorefrontItemResponse(Guid Id, string Name, string? ImageUrl, string ImageAlt,
    IReadOnlyList<StorefrontPriceRangeResponse> Prices);
public sealed record StorefrontPriceRangeResponse(string Currency, string MinimumAmount, string MaximumAmount);
