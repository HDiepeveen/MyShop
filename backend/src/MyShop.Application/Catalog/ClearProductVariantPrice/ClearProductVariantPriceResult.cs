namespace MyShop.Application.Catalog.ClearProductVariantPrice;

public sealed class ClearProductVariantPriceResult
{
    private ClearProductVariantPriceResult(ClearProductVariantPriceFailure? failure) => Failure = failure;

    public bool IsSuccess => Failure is null;
    public ClearProductVariantPriceFailure? Failure { get; }

    public static ClearProductVariantPriceResult Succeeded { get; } = new(null);

    public static ClearProductVariantPriceResult Failed(ClearProductVariantPriceFailure failure) => new(failure);
}
