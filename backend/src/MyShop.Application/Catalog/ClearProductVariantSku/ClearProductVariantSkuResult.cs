namespace MyShop.Application.Catalog.ClearProductVariantSku;

public sealed class ClearProductVariantSkuResult
{
    private ClearProductVariantSkuResult(ClearProductVariantSkuFailure? failure) => Failure = failure;

    public bool IsSuccess => Failure is null;
    public ClearProductVariantSkuFailure? Failure { get; }

    public static ClearProductVariantSkuResult Succeeded { get; } = new(null);

    public static ClearProductVariantSkuResult Failed(ClearProductVariantSkuFailure failure) => new(failure);
}
