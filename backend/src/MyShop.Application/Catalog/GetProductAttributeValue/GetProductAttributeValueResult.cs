using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductAttributeValue;

public sealed record ProductAttributeValueSnapshot(AttributeValue Value, Guid Revision);

public sealed class GetProductAttributeValueResult
{
    private GetProductAttributeValueResult(ProductAttributeValueSnapshot? snapshot, GetProductAttributeValueFailure? failure)
    {
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public ProductAttributeValueSnapshot? Snapshot { get; }
    public GetProductAttributeValueFailure? Failure { get; }

    public static GetProductAttributeValueResult Succeeded(ProductAttributeValueSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, null);
    }

    public static GetProductAttributeValueResult Failed(GetProductAttributeValueFailure failure) =>
        new(null, failure);
}
