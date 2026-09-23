using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductTypeAttribute;

public sealed record GetProductTypeAttributeQuery(ProductTypeId ProductTypeId, AttributeDefinitionId AttributeDefinitionId);
