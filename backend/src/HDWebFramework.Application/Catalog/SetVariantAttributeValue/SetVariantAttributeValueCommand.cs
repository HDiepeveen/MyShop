using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.SetVariantAttributeValue;

public sealed record SetVariantAttributeValueCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    AttributeDefinitionId AttributeDefinitionId,
    CatalogAttributeValueInput Value);
