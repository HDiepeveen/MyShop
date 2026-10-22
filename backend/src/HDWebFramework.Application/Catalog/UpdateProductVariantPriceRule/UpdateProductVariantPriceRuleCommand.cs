using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.UpdateProductVariantPriceRule;

public sealed record UpdateProductVariantPriceRuleCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    Guid PriceRuleId,
    string Name,
    PriceAdjustmentType AdjustmentType,
    decimal Value,
    int Priority,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null);
