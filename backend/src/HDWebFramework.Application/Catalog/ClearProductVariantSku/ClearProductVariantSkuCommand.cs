using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.ClearProductVariantSku;

public sealed record ClearProductVariantSkuCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId);
