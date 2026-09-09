namespace MyShop.Application.Catalog.RemoveVariantAttributeValue;

public sealed class RemoveVariantAttributeValueResult
{
    private RemoveVariantAttributeValueResult(RemoveVariantAttributeValueFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public RemoveVariantAttributeValueFailure? Failure { get; }

    public static RemoveVariantAttributeValueResult Succeeded { get; } = new(null);

    public static RemoveVariantAttributeValueResult Failed(RemoveVariantAttributeValueFailure failure) =>
        new(failure);
}
