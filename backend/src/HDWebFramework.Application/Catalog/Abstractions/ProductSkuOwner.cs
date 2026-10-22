using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public sealed record ProductSkuOwner(
    ProductId ProductId,
    ProductVariantId ProductVariantId);