using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductTypeAttribute;

public sealed class GetProductTypeAttributeResult
{
    private GetProductTypeAttributeResult(AttributeDefinition? attribute, GetProductTypeAttributeFailure? failure)
    {
        Attribute = attribute;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public AttributeDefinition? Attribute { get; }
    public GetProductTypeAttributeFailure? Failure { get; }

    public static GetProductTypeAttributeResult Succeeded(AttributeDefinition attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return new(attribute, null);
    }

    public static GetProductTypeAttributeResult Failed(GetProductTypeAttributeFailure failure) => new(null, failure);
}
