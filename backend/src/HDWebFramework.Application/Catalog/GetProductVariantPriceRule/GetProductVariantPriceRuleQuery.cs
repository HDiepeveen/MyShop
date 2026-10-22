using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.GetProductVariantPriceRule;

public sealed record GetProductVariantPriceRuleQuery(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    Guid PriceRuleId);
