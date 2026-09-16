using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProduct;

public sealed class GetProductResult
{
    private GetProductResult(ProductSnapshot? snapshot, GetProductFailure? failure)
    {
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public ProductSnapshot? Snapshot { get; }
    public GetProductFailure? Failure { get; }

    public static GetProductResult Succeeded(ProductSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, null);
    }

    public static GetProductResult Failed(GetProductFailure failure) =>
        new(null, failure);
}
