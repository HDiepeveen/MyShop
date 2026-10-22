using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RemoveVariantAttributeValue;

public sealed record RemoveVariantAttributeValueCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    AttributeDefinitionId AttributeDefinitionId);
