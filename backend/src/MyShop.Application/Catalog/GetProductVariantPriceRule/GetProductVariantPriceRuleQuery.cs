using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductVariantPriceRule;

public sealed record GetProductVariantPriceRuleQuery(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    Guid PriceRuleId);
