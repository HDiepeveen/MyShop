using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RemoveProductAttributeValue;

public sealed record RemoveProductAttributeValueCommand(
    ProductId ProductId,
    AttributeDefinitionId AttributeDefinitionId);
