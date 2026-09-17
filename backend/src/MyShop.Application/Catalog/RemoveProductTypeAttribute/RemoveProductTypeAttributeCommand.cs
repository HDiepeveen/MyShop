using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RemoveProductTypeAttribute;

public sealed record RemoveProductTypeAttributeCommand(
    ProductTypeId ProductTypeId,
    AttributeDefinitionId AttributeDefinitionId);
