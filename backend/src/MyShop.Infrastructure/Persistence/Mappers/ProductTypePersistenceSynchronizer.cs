using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class ProductTypePersistenceSynchronizer
{
    internal static void Synchronize(ProductType productType, ProductTypePersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(productType);
        ArgumentNullException.ThrowIfNull(persistence);

        if (persistence.Id == Guid.Empty)
            throw new InvalidOperationException("Persisted product type ID must not be empty.");
        if (productType.Id.Value != persistence.Id)
            throw new ArgumentException(
                "Domain and persistence product type IDs must match.", nameof(persistence));
        if (persistence.AttributeDefinitions is null)
            throw new InvalidOperationException("Persisted attribute definitions must not be null.");
        if (persistence.AttributeDefinitions.Any(attribute => attribute is null))
            throw new InvalidOperationException("Persisted attribute definitions must not contain null entries.");
        if (persistence.AttributeDefinitions.Any(attribute => attribute.Id == Guid.Empty))
            throw new InvalidOperationException("Persisted attribute definition IDs must not be empty.");
        if (persistence.AttributeDefinitions.GroupBy(attribute => attribute.Id).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Persisted attribute definition IDs must be unique.");
        if (persistence.AttributeDefinitions.Any(attribute => attribute.ProductTypeId != persistence.Id))
            throw new InvalidOperationException("Persisted attribute definitions must belong to the product type.");

        persistence.Name = productType.Name;

        var existing = persistence.AttributeDefinitions.ToDictionary(attribute => attribute.Id);
        var retainedIds = new HashSet<Guid>();
        foreach (var definition in productType.AttributeDefinitions)
        {
            var id = definition.Id.Value;
            retainedIds.Add(id);
            if (!existing.TryGetValue(id, out var persistedDefinition))
            {
                persistedDefinition = new AttributeDefinitionPersistence
                {
                    Id = id,
                    ProductTypeId = persistence.Id,
                    ProductType = persistence
                };
                persistence.AttributeDefinitions.Add(persistedDefinition);
            }

            persistedDefinition.Code = definition.Code.Value;
            persistedDefinition.DisplayName = definition.DisplayName;
            persistedDefinition.DataType = definition.DataType;
            persistedDefinition.Scope = definition.Scope;
            persistedDefinition.IsRequired = definition.IsRequired;
            persistedDefinition.IsFilterable = definition.IsFilterable;
        }

        foreach (var definition in existing.Values.Where(attribute => !retainedIds.Contains(attribute.Id)))
            persistence.AttributeDefinitions.Remove(definition);
    }
}
