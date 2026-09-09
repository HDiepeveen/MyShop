using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RemoveVariantAttributeValue;

public sealed record RemoveVariantAttributeValueCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    AttributeDefinitionId AttributeDefinitionId);
