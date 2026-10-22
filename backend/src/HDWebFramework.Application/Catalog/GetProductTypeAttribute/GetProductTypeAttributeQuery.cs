using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.GetProductTypeAttribute;

public sealed record GetProductTypeAttributeQuery(ProductTypeId ProductTypeId, AttributeDefinitionId AttributeDefinitionId);
