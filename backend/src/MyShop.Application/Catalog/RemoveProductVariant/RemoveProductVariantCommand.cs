using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RemoveProductVariant;

public sealed record RemoveProductVariantCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId);
