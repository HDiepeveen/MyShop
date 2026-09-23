using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ClearProductVariantPrice;

public sealed record ClearProductVariantPriceCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId);
