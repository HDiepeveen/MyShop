using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetProductAttributeValue;

public sealed record SetProductAttributeValueCommand(
    ProductId ProductId,
    AttributeDefinitionId AttributeDefinitionId,
    CatalogAttributeValueInput Value);
