using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetVariantAttributeValue;

public sealed record VariantAttributeValueSnapshot(AttributeValue Value, Guid Revision);

public sealed class GetVariantAttributeValueResult
{
    private GetVariantAttributeValueResult(VariantAttributeValueSnapshot? snapshot, GetVariantAttributeValueFailure? failure)
    {
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public VariantAttributeValueSnapshot? Snapshot { get; }
    public GetVariantAttributeValueFailure? Failure { get; }

    public static GetVariantAttributeValueResult Succeeded(VariantAttributeValueSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, null);
    }

    public static GetVariantAttributeValueResult Failed(GetVariantAttributeValueFailure failure) =>
        new(null, failure);
}
