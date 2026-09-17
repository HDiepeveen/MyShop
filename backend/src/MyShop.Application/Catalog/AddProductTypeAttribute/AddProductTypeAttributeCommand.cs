using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.AddProductTypeAttribute;

public sealed record AddProductTypeAttributeCommand(
    ProductTypeId ProductTypeId,
    string Code,
    string DisplayName,
    AttributeDataType DataType,
    bool IsRequired,
    bool IsFilterable,
    AttributeScope Scope);
