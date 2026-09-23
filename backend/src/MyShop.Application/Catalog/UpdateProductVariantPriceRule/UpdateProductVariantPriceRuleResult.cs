namespace MyShop.Application.Catalog.UpdateProductVariantPriceRule;

public sealed class UpdateProductVariantPriceRuleResult
{
    private UpdateProductVariantPriceRuleResult(UpdateProductVariantPriceRuleFailure? failure) => Failure = failure;

    public bool IsSuccess => Failure is null;
    public UpdateProductVariantPriceRuleFailure? Failure { get; }

    public static UpdateProductVariantPriceRuleResult Succeeded { get; } = new(null);

    public static UpdateProductVariantPriceRuleResult Failed(UpdateProductVariantPriceRuleFailure failure) => new(failure);
}
