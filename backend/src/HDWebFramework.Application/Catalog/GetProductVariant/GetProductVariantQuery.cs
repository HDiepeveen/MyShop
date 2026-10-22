using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.GetProductVariant;

public sealed record GetProductVariantQuery(ProductId ProductId, ProductVariantId ProductVariantId);
