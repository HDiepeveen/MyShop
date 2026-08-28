namespace MyShop.Application.Catalog.SetProductAttributeValue;

public enum SetProductAttributeValueFailure
{
    ProductNotFound,
    ProductTypeNotFound,
    AttributeDefinitionNotFound,
    WrongAttributeScope,
    WrongAttributeDataType
}
