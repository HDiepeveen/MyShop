using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RemoveProductVariantPriceRule;

public sealed record RemoveProductVariantPriceRuleCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    Guid PriceRuleId);
