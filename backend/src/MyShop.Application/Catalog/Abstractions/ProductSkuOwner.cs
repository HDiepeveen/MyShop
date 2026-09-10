using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public sealed record ProductSkuOwner(
    ProductId ProductId,
    ProductVariantId ProductVariantId);