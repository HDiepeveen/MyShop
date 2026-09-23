using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductAttributeValue;

public sealed record GetProductAttributeValueQuery(ProductId ProductId, AttributeDefinitionId AttributeDefinitionId);
