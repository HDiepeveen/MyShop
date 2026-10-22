using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.ClearProductVariantPrice;

public sealed record ClearProductVariantPriceCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId);
