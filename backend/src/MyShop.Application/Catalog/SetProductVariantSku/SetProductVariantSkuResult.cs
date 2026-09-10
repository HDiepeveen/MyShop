namespace MyShop.Application.Catalog.SetProductVariantSku;

public sealed class SetProductVariantSkuResult
{
    private SetProductVariantSkuResult(SetProductVariantSkuFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public SetProductVariantSkuFailure? Failure { get; }

    public static SetProductVariantSkuResult Succeeded { get; } = new(null);

    public static SetProductVariantSkuResult Failed(SetProductVariantSkuFailure failure) =>
        new(failure);
}