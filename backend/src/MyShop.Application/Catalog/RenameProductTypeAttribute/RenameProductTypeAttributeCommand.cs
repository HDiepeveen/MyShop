using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RenameProductTypeAttribute;

public sealed record RenameProductTypeAttributeCommand(
    ProductTypeId ProductTypeId,
    AttributeDefinitionId AttributeDefinitionId,
    string DisplayName);
