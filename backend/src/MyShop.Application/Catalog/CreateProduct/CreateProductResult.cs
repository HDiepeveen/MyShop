using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.CreateProduct;

public sealed class CreateProductResult
{
    private CreateProductResult(ProductSnapshot? snapshot, CreateProductFailure? failure)
    {
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public ProductSnapshot? Snapshot { get; }
    public CreateProductFailure? Failure { get; }

    public static CreateProductResult Succeeded(ProductSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, null);
    }

    public static CreateProductResult Failed(CreateProductFailure failure) =>
        new(null, failure);
}
