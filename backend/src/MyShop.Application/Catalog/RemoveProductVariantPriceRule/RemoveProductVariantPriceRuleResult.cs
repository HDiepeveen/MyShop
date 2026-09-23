namespace MyShop.Application.Catalog.RemoveProductVariantPriceRule;

public sealed class RemoveProductVariantPriceRuleResult
{
    private RemoveProductVariantPriceRuleResult(RemoveProductVariantPriceRuleFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public RemoveProductVariantPriceRuleFailure? Failure { get; }

    public static RemoveProductVariantPriceRuleResult Succeeded { get; } = new(null);

    public static RemoveProductVariantPriceRuleResult Failed(RemoveProductVariantPriceRuleFailure failure) =>
        new(failure);
}
