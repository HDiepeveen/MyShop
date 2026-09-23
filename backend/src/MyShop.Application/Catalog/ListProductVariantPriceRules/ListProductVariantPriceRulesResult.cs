using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ListProductVariantPriceRules;

public sealed record PriceRuleListSnapshot(IReadOnlyList<PriceRule> Rules, Guid Revision);

public sealed class ListProductVariantPriceRulesResult
{
    private ListProductVariantPriceRulesResult(PriceRuleListSnapshot? snapshot, ListProductVariantPriceRulesFailure? failure)
    {
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public PriceRuleListSnapshot? Snapshot { get; }
    public ListProductVariantPriceRulesFailure? Failure { get; }

    public static ListProductVariantPriceRulesResult Succeeded(PriceRuleListSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, null);
    }

    public static ListProductVariantPriceRulesResult Failed(ListProductVariantPriceRulesFailure failure) =>
        new(null, failure);
}
