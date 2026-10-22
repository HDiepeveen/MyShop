using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.GetProductVariantPrice;

public sealed record GetProductVariantPriceQuery(
    ProductId ProductId, ProductVariantId ProductVariantId, DateTimeOffset At);
