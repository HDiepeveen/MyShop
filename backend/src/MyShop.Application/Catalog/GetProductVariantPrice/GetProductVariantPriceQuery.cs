using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductVariantPrice;

public sealed record GetProductVariantPriceQuery(
    ProductId ProductId, ProductVariantId ProductVariantId, DateTimeOffset At);
