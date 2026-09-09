using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RemoveProductAttributeValue;

public sealed record RemoveProductAttributeValueCommand(
    ProductId ProductId,
    AttributeDefinitionId AttributeDefinitionId);
