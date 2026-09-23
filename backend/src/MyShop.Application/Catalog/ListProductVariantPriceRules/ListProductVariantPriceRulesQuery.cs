using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ListProductVariantPriceRules;

public sealed record ListProductVariantPriceRulesQuery(ProductId ProductId, ProductVariantId ProductVariantId);
