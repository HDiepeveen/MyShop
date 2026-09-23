using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class ProductPersistenceMapper
{
    internal static ProductSnapshot ToSnapshot(ProductPersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(persistence);

        var productId = ProductId.From(persistence.Id);
        var productTypeId = ProductTypeId.From(persistence.ProductTypeId);
        var token = ProductConcurrencyToken.Create(productId, persistence.Version);

        var priceRuleIds = new HashSet<Guid>();
        var variants = OrderByOrdinal(
                persistence.Variants,
                variant => variant.Ordinal,
                persistence.Id,
                "variants")
            .Select(variant => ToDomain(variant, persistence.Id, priceRuleIds))
            .ToList();

        var categories = OrderByOrdinal(
                persistence.Categories,
                category => category.Ordinal,
                persistence.Id,
                "categories")
            .Select(category =>
            {
                if (category.ProductId != persistence.Id)
                    throw InvalidStructure(persistence.Id,
                        $"Category '{category.CategoryId}' belongs to Product '{category.ProductId}'.");
                return CategoryId.From(category.CategoryId);
            })
            .ToList();

        var attributeValues = OrderByOrdinal(
                persistence.AttributeValues,
                value => value.Ordinal,
                persistence.Id,
                "product attribute values")
            .Select(value =>
            {
                if (value.ProductId != persistence.Id)
                    throw InvalidStructure(persistence.Id,
                        $"Attribute value '{value.AttributeDefinitionId}' belongs to Product '{value.ProductId}'.");
                return AttributeValuePersistenceMapper.ToDomain(value);
            })
            .ToList();

        var product = Product.Rehydrate(
            productId,
            productTypeId,
            persistence.Name,
            variants,
            categories,
            attributeValues);

        return new ProductSnapshot(product, token);
    }

    private static ProductVariant ToDomain(ProductVariantPersistence persistence, Guid productId, HashSet<Guid> priceRuleIds)
    {
        if (persistence.ProductId != productId)
            throw InvalidStructure(productId,
                $"Variant '{persistence.Id}' belongs to Product '{persistence.ProductId}'.");

        var attributeValues = OrderByOrdinal(
                persistence.AttributeValues,
                value => value.Ordinal,
                productId,
                $"attribute values for Variant '{persistence.Id}'")
            .Select(value =>
            {
                if (value.ProductVariantId != persistence.Id)
                    throw InvalidStructure(productId,
                        $"Attribute value '{value.AttributeDefinitionId}' belongs to Variant " +
                        $"'{value.ProductVariantId}' instead of '{persistence.Id}'.");
                return AttributeValuePersistenceMapper.ToDomain(value);
            })
            .ToList();

        if (persistence.PriceRules is null)
            throw InvalidStructure(productId, "The price rules collection is null.");
        foreach (var rule in persistence.PriceRules)
        {
            if (rule is null)
                throw InvalidStructure(productId, "The price rules collection contains a null entry.");
            if (rule.Id == Guid.Empty || !priceRuleIds.Add(rule.Id))
                throw InvalidStructure(productId, "Price rule IDs must be non-empty and unique across variants.");
            if (rule.ProductVariantId != persistence.Id)
                throw InvalidStructure(productId, $"Price rule '{rule.Id}' belongs to another variant.");
        }

        return ProductVariant.Rehydrate(
            ProductVariantId.From(persistence.Id),
            persistence.Name,
            persistence.Sku is null ? null : Sku.Create(persistence.Sku),
            attributeValues,
            persistence.PriceAmount is null && persistence.PriceCurrency is null
                ? null
                : Money.Create(persistence.PriceAmount ?? throw InvalidStructure(productId, "Variant price amount is missing."), persistence.PriceCurrency ?? throw InvalidStructure(productId, "Variant price currency is missing.")),
            persistence.PriceRules.OrderBy(rule => rule.Priority).Select(rule => PriceRule.Rehydrate(rule.Id, rule.Name, (PriceAdjustmentType)rule.AdjustmentType, rule.Value, rule.Priority, rule.StartsAt, rule.EndsAt)));
    }

    private static IReadOnlyList<T> OrderByOrdinal<T>(
        ICollection<T>? values,
        Func<T, int> ordinalSelector,
        Guid productId,
        string collectionName)
        where T : class
    {
        if (values is null)
            throw InvalidStructure(productId, $"The {collectionName} collection is null.");
        if (values.Any(value => value is null))
            throw InvalidStructure(productId, $"The {collectionName} collection contains a null entry.");
        if (values.GroupBy(ordinalSelector).Any(group => group.Count() > 1))
            throw InvalidStructure(productId, $"The {collectionName} collection contains duplicate ordinals.");

        return values.OrderBy(ordinalSelector).ToList();
    }

    private static InvalidOperationException InvalidStructure(Guid productId, string message) =>
        new($"Invalid persisted Product '{productId}': {message}");
}
