using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.AddProductVariantPriceRule;

public sealed record AddProductVariantPriceRuleCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    string Name,
    PriceAdjustmentType AdjustmentType,
    decimal Value,
    int Priority,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null);
