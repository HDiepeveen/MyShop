namespace MyShop.Application.Catalog.RenameProductVariant;

public sealed class RenameProductVariantResult
{
    private RenameProductVariantResult(RenameProductVariantFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public RenameProductVariantFailure? Failure { get; }

    public static RenameProductVariantResult Succeeded { get; } = new(null);

    public static RenameProductVariantResult Failed(RenameProductVariantFailure failure) =>
        new(failure);
}
