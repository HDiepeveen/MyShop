using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.SetProductVariantSku;

public sealed record SetProductVariantSkuCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    string Sku);