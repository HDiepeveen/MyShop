using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RemoveProductVariantPriceRule;

public sealed record RemoveProductVariantPriceRuleCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    Guid PriceRuleId);
