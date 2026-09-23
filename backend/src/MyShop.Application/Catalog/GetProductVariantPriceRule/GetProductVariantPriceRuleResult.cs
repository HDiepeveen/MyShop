using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductVariantPriceRule;

public sealed record PriceRuleSnapshot(PriceRule Rule, Guid Revision);

public sealed class GetProductVariantPriceRuleResult
{
    private GetProductVariantPriceRuleResult(PriceRuleSnapshot? snapshot, GetProductVariantPriceRuleFailure? failure)
    {
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public PriceRuleSnapshot? Snapshot { get; }
    public GetProductVariantPriceRuleFailure? Failure { get; }

    public static GetProductVariantPriceRuleResult Succeeded(PriceRuleSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, null);
    }

    public static GetProductVariantPriceRuleResult Failed(GetProductVariantPriceRuleFailure failure) =>
        new(null, failure);
}
