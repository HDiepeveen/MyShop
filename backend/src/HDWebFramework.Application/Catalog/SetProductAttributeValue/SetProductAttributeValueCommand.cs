using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.SetProductAttributeValue;

public sealed record SetProductAttributeValueCommand(
    ProductId ProductId,
    AttributeDefinitionId AttributeDefinitionId,
    CatalogAttributeValueInput Value);
