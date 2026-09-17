using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ConfigureProductTypeAttribute;

public sealed record ConfigureProductTypeAttributeCommand(
    ProductTypeId ProductTypeId,
    AttributeDefinitionId AttributeDefinitionId,
    bool IsRequired,
    bool IsFilterable);
