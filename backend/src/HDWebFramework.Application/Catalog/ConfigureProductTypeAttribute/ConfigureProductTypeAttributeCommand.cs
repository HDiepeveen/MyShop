using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.ConfigureProductTypeAttribute;

public sealed record ConfigureProductTypeAttributeCommand(
    ProductTypeId ProductTypeId,
    AttributeDefinitionId AttributeDefinitionId,
    bool IsRequired,
    bool IsFilterable);
