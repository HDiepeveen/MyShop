namespace MyShop.Application.Catalog.RemoveProductVariant;

public sealed class RemoveProductVariantResult
{
    private RemoveProductVariantResult(RemoveProductVariantFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public RemoveProductVariantFailure? Failure { get; }

    public static RemoveProductVariantResult Succeeded { get; } = new(null);

    public static RemoveProductVariantResult Failed(RemoveProductVariantFailure failure) =>
        new(failure);
}
