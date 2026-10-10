using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using MyShop.Application.Catalog.GetStorefrontPrices;
using MyShop.Application.Catalog.BrowseStorefront;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetStorefrontProduct;
using MyShop.Application.Catalog.ListStorefrontCategories;
using MyShop.Domain.Catalog;
using MyShop.Api.Checkout;

namespace MyShop.Api.Storefront;

public static class StorefrontEndpoints
{
    public static void MapStorefront(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/shop/settings", async ([FromServices] MyShop.Application.Catalog.Seo.ManageSeo useCase, CancellationToken ct) =>
        {
            var settings = await useCase.GetSettingsAsync(ct);
            return Results.Ok(new { settings.ShopName, settings.WelcomeText, settings.Introduction });
        }).AllowAnonymous();
        endpoints.MapCartQuote();
        endpoints.MapPlaceOrder();
        endpoints.MapStartOnlinePayment();
        endpoints.MapCompleteOnlinePayment();
        endpoints.MapMollieWebhook();
        endpoints.MapGet("/api/shop/categories", async (
            [FromServices] ListStorefrontCategories useCase, CancellationToken cancellationToken) =>
            Results.Ok(await useCase.ExecuteAsync(cancellationToken))).AllowAnonymous();
        endpoints.MapGet("/api/shop/products/{productId}/prices", async (string productId,
            [FromServices] GetStorefrontPrices useCase, [FromServices] MyShop.Application.Catalog.Seo.ICatalogSeoStore seo, CancellationToken cancellationToken) =>
        {
            var resolved = await seo.ResolveProductAsync(productId, cancellationToken);
            if (resolved is not Guid id) return Results.NotFound();
            var prices = await useCase.ExecuteAsync(new(ProductId.From(id), DateTimeOffset.UtcNow), cancellationToken);
            return prices is null ? (IResult)Results.NotFound() : Results.Ok(new StorefrontPricesResponse(
                prices.At, prices.Variants.Select(price => new StorefrontVariantPriceResponse(price.VariantId,
                    price.Amount?.ToString("F2", CultureInfo.InvariantCulture), price.Currency)).ToList()));
        }).AllowAnonymous();
        endpoints.MapGet("/api/shop/products", async (int? offset, int? limit, string? search, Guid? categoryId,
            string? sort, bool? availableOnly,
            [FromServices] BrowseStorefront useCase, CancellationToken cancellationToken) =>
        {
            try
            {
                var ordering = sort switch { null or "" or "nameAsc" => StorefrontSort.NameAscending,
                    "nameDesc" => StorefrontSort.NameDescending, _ => throw new ArgumentException("Unknown sort order.") };
                CategoryId? category = categoryId is null ? null : CategoryId.From(categoryId.Value);
                var page = await useCase.ExecuteAsync(
                    new(offset ?? 0, limit ?? 20, search, category, DateTimeOffset.UtcNow, ordering, availableOnly ?? false),
                    cancellationToken);
                return Results.Ok(new StorefrontPageResponse(page.At,
                    page.Items.Select(item => new StorefrontItemResponse(item.Id, item.Name,
                        item.ImageUrl, item.ImageAlt, item.IsAvailable, item.Prices.Select(price =>
                            new StorefrontPriceRangeResponse(price.Currency,
                                price.MinimumAmount.ToString("F2", CultureInfo.InvariantCulture),
                                price.MaximumAmount.ToString("F2", CultureInfo.InvariantCulture))).ToList()) { WebAddress = item.WebAddress })
                        .ToList(), page.TotalCount, page.Offset, page.Limit) { WelcomeText = page.WelcomeText, Introduction = page.Introduction, Heading = page.Heading, SeoTitle = page.SeoTitle });
            }
            catch (ArgumentException) { return (IResult)Results.BadRequest(); }
        }).AllowAnonymous();
        endpoints.MapGet("/api/shop/products/{productId}", async (string productId,
            [FromServices] GetStorefrontProduct useCase, [FromServices] MyShop.Application.Catalog.Seo.ICatalogSeoStore seo, CancellationToken cancellationToken) =>
        {
            var resolved = await seo.ResolveProductAsync(productId, cancellationToken);
            if (resolved is not Guid id) return Results.NotFound();
            var product = await useCase.ExecuteAsync(new(ProductId.From(id)), cancellationToken);
            if (product is null) return Results.NotFound();
            var metadata = await seo.GetProductAsync(id, cancellationToken);
            if (metadata is null || !metadata.Published) return Results.NotFound();
            return Results.Ok(product with { SeoTitle = metadata!.ResolvedTitle, SeoDescription = metadata.ResolvedDescription, WebAddress = metadata.ResolvedAddress, AboutHeading = metadata.ResolvedAboutHeading, AttributesHeading = metadata.ResolvedAttributesHeading });
        }).AllowAnonymous();
    }
}

public sealed record StorefrontPageResponse(DateTimeOffset At, IReadOnlyList<StorefrontItemResponse> Items,
    int TotalCount, int Offset, int Limit)
{
    public string WelcomeText { get; init; } = "Welkom bij MyShop";
    public string Introduction { get; init; } = "Bekijk onze producten en kies de variant die bij je past.";
    public string Heading { get; init; } = "Ontdek ons assortiment";
    public string SeoTitle { get; init; } = "Assortiment · MyShop";
}
public sealed record StorefrontItemResponse(Guid Id, string Name, string? ImageUrl, string ImageAlt,
    bool IsAvailable, IReadOnlyList<StorefrontPriceRangeResponse> Prices)
{
    public string? WebAddress { get; init; }
}
public sealed record StorefrontPriceRangeResponse(string Currency, string MinimumAmount, string MaximumAmount);
public sealed record StorefrontPricesResponse(DateTimeOffset At,
    IReadOnlyList<StorefrontVariantPriceResponse> Variants);
public sealed record StorefrontVariantPriceResponse(Guid VariantId, string? Amount, string? Currency);
