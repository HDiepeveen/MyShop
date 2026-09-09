namespace MyShop.Application.Catalog.SetVariantAttributeValue;

public enum SetVariantAttributeValueFailure
{
    ProductNotFound,
    VariantNotFound,
    ProductTypeNotFound,
    AttributeDefinitionNotFound,
    WrongAttributeScope,
    WrongAttributeDataType
}
