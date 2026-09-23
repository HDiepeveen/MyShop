namespace MyShop.Application.Catalog.AddProductVariantPriceRule;

public sealed class AddProductVariantPriceRuleResult
{
    private AddProductVariantPriceRuleResult(Guid? priceRuleId, AddProductVariantPriceRuleFailure? failure)
    {
        PriceRuleId = priceRuleId;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public Guid? PriceRuleId { get; }
    public AddProductVariantPriceRuleFailure? Failure { get; }

    public static AddProductVariantPriceRuleResult Succeeded(Guid priceRuleId)
    {
        if (priceRuleId == Guid.Empty)
            throw new ArgumentException("Price rule ID must not be empty.", nameof(priceRuleId));
        return new(priceRuleId, null);
    }

    public static AddProductVariantPriceRuleResult Failed(AddProductVariantPriceRuleFailure failure) =>
        new(null, failure);
}
