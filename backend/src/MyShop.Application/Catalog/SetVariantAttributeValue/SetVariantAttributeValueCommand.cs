using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetVariantAttributeValue;

public sealed record SetVariantAttributeValueCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    AttributeDefinitionId AttributeDefinitionId,
    CatalogAttributeValueInput Value);
