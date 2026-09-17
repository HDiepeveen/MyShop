namespace MyShop.Api.Catalog;

public enum ApiAttributeDataType
{
    Text = 0,
    Integer = 1,
    Decimal = 2,
    Boolean = 3,
    Date = 4,
    Choice = 5,
    MultiChoice = 6
}

public enum ApiAttributeScope
{
    Product = 0,
    Variant = 1
}
