using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RenameProductTypeAttribute;

public sealed record RenameProductTypeAttributeCommand(
    ProductTypeId ProductTypeId,
    AttributeDefinitionId AttributeDefinitionId,
    string DisplayName);
