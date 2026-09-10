using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetProductVariantSku;

public sealed record SetProductVariantSkuCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    string Sku);