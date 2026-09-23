using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductVariant;

public sealed record GetProductVariantQuery(ProductId ProductId, ProductVariantId ProductVariantId);
