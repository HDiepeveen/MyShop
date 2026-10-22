using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RemoveProductTypeAttribute;

public sealed record RemoveProductTypeAttributeCommand(
    ProductTypeId ProductTypeId,
    AttributeDefinitionId AttributeDefinitionId);
