using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.AddProductVariantPriceRule;

public sealed record AddProductVariantPriceRuleCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    string Name,
    PriceAdjustmentType AdjustmentType,
    decimal Value,
    int Priority,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null);
