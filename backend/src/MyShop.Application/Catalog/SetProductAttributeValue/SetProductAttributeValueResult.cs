namespace MyShop.Application.Catalog.SetProductAttributeValue;

public sealed class SetProductAttributeValueResult
{
    private SetProductAttributeValueResult(SetProductAttributeValueFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public SetProductAttributeValueFailure? Failure { get; }

    public static SetProductAttributeValueResult Succeeded { get; } = new(null);

    public static SetProductAttributeValueResult Failed(SetProductAttributeValueFailure failure) =>
        new(failure);
}
