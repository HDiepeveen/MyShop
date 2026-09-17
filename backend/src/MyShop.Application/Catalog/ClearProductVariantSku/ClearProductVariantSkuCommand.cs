using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ClearProductVariantSku;

public sealed record ClearProductVariantSkuCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId);
