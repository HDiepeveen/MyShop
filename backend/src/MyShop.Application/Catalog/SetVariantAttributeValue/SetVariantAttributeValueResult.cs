namespace MyShop.Application.Catalog.SetVariantAttributeValue;

public sealed class SetVariantAttributeValueResult
{
    private SetVariantAttributeValueResult(SetVariantAttributeValueFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public SetVariantAttributeValueFailure? Failure { get; }

    public static SetVariantAttributeValueResult Succeeded { get; } = new(null);

    public static SetVariantAttributeValueResult Failed(SetVariantAttributeValueFailure failure) =>
        new(failure);
}
