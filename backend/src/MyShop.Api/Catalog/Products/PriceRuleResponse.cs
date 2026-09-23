using MyShop.Domain.Catalog;

namespace MyShop.Api.Catalog.Products;

public sealed record PriceRuleResponse(
    Guid Id,
    string Name,
    int AdjustmentType,
    decimal Value,
    int Priority,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt)
{
    internal static PriceRuleResponse FromDomain(PriceRule rule) =>
        new(rule.Id, rule.Name, (int)rule.AdjustmentType, rule.Value,
            rule.Priority, rule.StartsAt, rule.EndsAt);
}
