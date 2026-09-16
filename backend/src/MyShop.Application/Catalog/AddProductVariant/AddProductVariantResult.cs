using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.AddProductVariant;

public sealed class AddProductVariantResult
{
    private AddProductVariantResult(ProductSnapshot? snapshot, AddProductVariantFailure? failure)
    {
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public ProductSnapshot? Snapshot { get; }
    public AddProductVariantFailure? Failure { get; }

    public static AddProductVariantResult Succeeded(ProductSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, null);
    }

    public static AddProductVariantResult Failed(AddProductVariantFailure failure) =>
        new(null, failure);
}
