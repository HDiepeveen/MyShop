namespace MyShop.Application.Catalog.RemoveProductAttributeValue;

public sealed class RemoveProductAttributeValueResult
{
    private RemoveProductAttributeValueResult(RemoveProductAttributeValueFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public RemoveProductAttributeValueFailure? Failure { get; }

    public static RemoveProductAttributeValueResult Succeeded { get; } = new(null);

    public static RemoveProductAttributeValueResult Failed(RemoveProductAttributeValueFailure failure) =>
        new(failure);
}
