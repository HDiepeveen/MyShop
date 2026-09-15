using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class ProductTypePersistenceMapper
{
    internal static ProductType ToDomain(ProductTypePersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(persistence);

        var definitions = persistence.AttributeDefinitions.Select(attribute =>
            AttributeDefinition.Rehydrate(
                AttributeDefinitionId.From(attribute.Id),
                AttributeCode.Create(attribute.Code),
                attribute.DisplayName,
                attribute.DataType,
                attribute.IsRequired,
                attribute.IsFilterable,
                attribute.Scope));

        return ProductType.Rehydrate(
            ProductTypeId.From(persistence.Id),
            persistence.Name,
            definitions);
    }
}