using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductVariant;

public sealed record ProductVariantSnapshot(ProductVariant Variant, Guid Revision);

public sealed class GetProductVariantResult
{
    private GetProductVariantResult(ProductVariantSnapshot? snapshot, GetProductVariantFailure? failure)
    {
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public ProductVariantSnapshot? Snapshot { get; }
    public GetProductVariantFailure? Failure { get; }

    public static GetProductVariantResult Succeeded(ProductVariantSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, null);
    }

    public static GetProductVariantResult Failed(GetProductVariantFailure failure) =>
        new(null, failure);
}
