using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.GetVariantAttributeValue;

public sealed record GetVariantAttributeValueQuery(ProductId ProductId, ProductVariantId ProductVariantId, AttributeDefinitionId AttributeDefinitionId);
