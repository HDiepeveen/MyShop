using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.ListProductVariantPriceRules;

public sealed record ListProductVariantPriceRulesQuery(ProductId ProductId, ProductVariantId ProductVariantId);
