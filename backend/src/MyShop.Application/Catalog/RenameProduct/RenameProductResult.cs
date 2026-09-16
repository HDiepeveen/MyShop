namespace MyShop.Application.Catalog.RenameProduct;

public sealed class RenameProductResult
{
    private RenameProductResult(RenameProductFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public RenameProductFailure? Failure { get; }

    public static RenameProductResult Succeeded { get; } = new(null);

    public static RenameProductResult Failed(RenameProductFailure failure) =>
        new(failure);
}
