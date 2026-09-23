using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetVariantAttributeValue;

public sealed record GetVariantAttributeValueQuery(ProductId ProductId, ProductVariantId ProductVariantId, AttributeDefinitionId AttributeDefinitionId);
