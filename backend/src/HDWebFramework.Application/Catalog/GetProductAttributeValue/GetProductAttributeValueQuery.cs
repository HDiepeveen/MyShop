using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.GetProductAttributeValue;

public sealed record GetProductAttributeValueQuery(ProductId ProductId, AttributeDefinitionId AttributeDefinitionId);
