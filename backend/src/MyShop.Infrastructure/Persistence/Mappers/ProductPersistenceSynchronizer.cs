using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class ProductPersistenceSynchronizer
{
    internal static void Synchronize(Product product, ProductPersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(persistence);

        ValidateGraph(product, persistence);

        persistence.ProductTypeId = product.ProductTypeId.Value;
        persistence.Name = product.Name;

        SynchronizeVariants(product, persistence);
        SynchronizeCategories(product, persistence);
        SynchronizeProductAttributeValues(product, persistence);
    }

    private static void ValidateGraph(Product product, ProductPersistence persistence)
    {
        if (persistence.Id == Guid.Empty)
            throw InvalidStructure(persistence.Id, "Product ID must not be empty.");
        if (product.Id.Value != persistence.Id)
            throw new ArgumentException("Domain and persistence Product IDs must match.", nameof(persistence));

        var variants = ValidateCollection(persistence.Variants, persistence.Id, "variants");
        ValidateNonEmptyUnique(
            variants,
            variant => variant.Id,
            persistence.Id,
            "variant IDs");
        ValidateUniqueOrdinals(variants, variant => variant.Ordinal, persistence.Id, "variant ordinals");
        foreach (var variant in variants)
        {
            if (variant.ProductId != persistence.Id)
                throw InvalidStructure(persistence.Id,
                    $"Variant '{variant.Id}' belongs to Product '{variant.ProductId}'.");

            ValidateVariantAttributeValues(variant, persistence.Id);
        }

        var categories = ValidateCollection(persistence.Categories, persistence.Id, "categories");
        ValidateNonEmptyUnique(
            categories,
            category => category.CategoryId,
            persistence.Id,
            "category IDs");
        ValidateUniqueOrdinals(categories, category => category.Ordinal, persistence.Id, "category ordinals");
        foreach (var category in categories)
        {
            if (category.ProductId != persistence.Id)
                throw InvalidStructure(persistence.Id,
                    $"Category '{category.CategoryId}' belongs to Product '{category.ProductId}'.");
        }

        ValidateProductAttributeValues(persistence, persistence.Id);
    }

    private static void ValidateProductAttributeValues(ProductPersistence persistence, Guid productId)
    {
        var values = ValidateCollection(persistence.AttributeValues, productId, "product attribute values");
        ValidateNonEmptyUnique(
            values,
            value => value.AttributeDefinitionId,
            productId,
            "product attribute definition IDs");
        ValidateUniqueOrdinals(
            values,
            value => value.Ordinal,
            productId,
            "product attribute value ordinals");

        foreach (var value in values)
        {
            if (value.ProductId != productId)
                throw InvalidStructure(productId,
                    $"Attribute value '{value.AttributeDefinitionId}' belongs to Product '{value.ProductId}'.");

            ValidateProductMultiChoiceValues(value, productId);
        }
    }

    private static void ValidateVariantAttributeValues(ProductVariantPersistence variant, Guid productId)
    {
        var values = ValidateCollection(
            variant.AttributeValues,
            productId,
            $"attribute values for Variant '{variant.Id}'");
        ValidateNonEmptyUnique(
            values,
            value => value.AttributeDefinitionId,
            productId,
            $"attribute definition IDs for Variant '{variant.Id}'");
        ValidateUniqueOrdinals(
            values,
            value => value.Ordinal,
            productId,
            $"attribute value ordinals for Variant '{variant.Id}'");

        foreach (var value in values)
        {
            if (value.ProductVariantId != variant.Id)
                throw InvalidStructure(productId,
                    $"Attribute value '{value.AttributeDefinitionId}' belongs to Variant " +
                    $"'{value.ProductVariantId}' instead of '{variant.Id}'.");

            ValidateVariantMultiChoiceValues(value, productId);
        }
    }

    private static void ValidateProductMultiChoiceValues(
        ProductAttributeValuePersistence parent,
        Guid productId)
    {
        var children = ValidateCollection(
            parent.MultiChoiceValues,
            productId,
            $"multi-choice values for Product attribute '{parent.AttributeDefinitionId}'");
        ValidateUniqueOrdinals(
            children,
            child => child.Ordinal,
            productId,
            $"multi-choice ordinals for Product attribute '{parent.AttributeDefinitionId}'");

        foreach (var child in children)
        {
            if (child.ProductId == Guid.Empty || child.ProductId != parent.ProductId)
                throw InvalidStructure(productId,
                    $"Multi-choice child owner does not match Product attribute '{parent.AttributeDefinitionId}'.");
            if (child.AttributeDefinitionId == Guid.Empty ||
                child.AttributeDefinitionId != parent.AttributeDefinitionId)
                throw InvalidStructure(productId,
                    $"Multi-choice child definition does not match Product attribute '{parent.AttributeDefinitionId}'.");
        }
    }

    private static void ValidateVariantMultiChoiceValues(
        ProductVariantAttributeValuePersistence parent,
        Guid productId)
    {
        var children = ValidateCollection(
            parent.MultiChoiceValues,
            productId,
            $"multi-choice values for Variant attribute '{parent.AttributeDefinitionId}'");
        ValidateUniqueOrdinals(
            children,
            child => child.Ordinal,
            productId,
            $"multi-choice ordinals for Variant attribute '{parent.AttributeDefinitionId}'");

        foreach (var child in children)
        {
            if (child.ProductVariantId == Guid.Empty || child.ProductVariantId != parent.ProductVariantId)
                throw InvalidStructure(productId,
                    $"Multi-choice child owner does not match Variant attribute '{parent.AttributeDefinitionId}'.");
            if (child.AttributeDefinitionId == Guid.Empty ||
                child.AttributeDefinitionId != parent.AttributeDefinitionId)
                throw InvalidStructure(productId,
                    $"Multi-choice child definition does not match Variant attribute '{parent.AttributeDefinitionId}'.");
        }
    }

    private static void SynchronizeVariants(Product product, ProductPersistence persistence)
    {
        var existing = persistence.Variants.ToDictionary(variant => variant.Id);
        var retainedIds = new HashSet<Guid>();
        var ordinal = 0;

        foreach (var domainVariant in product.Variants)
        {
            var id = domainVariant.Id.Value;
            retainedIds.Add(id);

            if (!existing.TryGetValue(id, out var persistedVariant))
            {
                persistedVariant = new ProductVariantPersistence
                {
                    Id = id,
                    ProductId = persistence.Id,
                    Product = persistence
                };
                persistence.Variants.Add(persistedVariant);
            }

            persistedVariant.Name = domainVariant.Name;
            persistedVariant.Sku = domainVariant.Sku?.Value;
            persistedVariant.PriceAmount = domainVariant.Price?.Amount;
            persistedVariant.PriceCurrency = domainVariant.Price?.Currency;
            persistedVariant.Ordinal = ordinal++;
            SynchronizePriceRules(domainVariant, persistedVariant);
            SynchronizeVariantAttributeValues(domainVariant, persistedVariant);
        }

        foreach (var variant in existing.Values.Where(variant => !retainedIds.Contains(variant.Id)))
            persistence.Variants.Remove(variant);
    }

    private static void SynchronizePriceRules(ProductVariant domainVariant, ProductVariantPersistence persistence)
    {
        var existing = persistence.PriceRules.ToDictionary(rule => rule.Id);
        var retained = new HashSet<Guid>();
        foreach (var domainRule in domainVariant.PriceRules)
        {
            retained.Add(domainRule.Id);
            if (!existing.TryGetValue(domainRule.Id, out var rule))
            {
                rule = new PriceRulePersistence { Id = domainRule.Id, ProductVariantId = persistence.Id, ProductVariant = persistence };
                persistence.PriceRules.Add(rule);
            }
            rule.Name = domainRule.Name; rule.AdjustmentType = (int)domainRule.AdjustmentType; rule.Value = domainRule.Value;
            rule.Priority = domainRule.Priority; rule.StartsAt = domainRule.StartsAt; rule.EndsAt = domainRule.EndsAt;
        }
        foreach (var rule in existing.Values.Where(rule => !retained.Contains(rule.Id))) persistence.PriceRules.Remove(rule);
    }

    private static void SynchronizeCategories(Product product, ProductPersistence persistence)
    {
        var existing = persistence.Categories.ToDictionary(category => category.CategoryId);
        var retainedIds = new HashSet<Guid>();
        var ordinal = 0;

        foreach (var domainCategoryId in product.CategoryIds)
        {
            var categoryId = domainCategoryId.Value;
            retainedIds.Add(categoryId);

            if (!existing.TryGetValue(categoryId, out var persistedCategory))
            {
                persistedCategory = new ProductCategoryPersistence
                {
                    ProductId = persistence.Id,
                    CategoryId = categoryId,
                    Product = persistence
                };
                persistence.Categories.Add(persistedCategory);
            }

            persistedCategory.Ordinal = ordinal++;
        }

        foreach (var category in existing.Values.Where(category => !retainedIds.Contains(category.CategoryId)))
            persistence.Categories.Remove(category);
    }

    private static void SynchronizeProductAttributeValues(Product product, ProductPersistence persistence)
    {
        var existing = persistence.AttributeValues.ToDictionary(value => value.AttributeDefinitionId);
        var retainedIds = new HashSet<Guid>();
        var ordinal = 0;

        foreach (var domainValue in product.AttributeValues)
        {
            var definitionId = domainValue.AttributeDefinitionId.Value;
            retainedIds.Add(definitionId);

            if (!existing.TryGetValue(definitionId, out var persistedValue))
            {
                persistedValue = new ProductAttributeValuePersistence
                {
                    ProductId = persistence.Id,
                    AttributeDefinitionId = definitionId,
                    Product = persistence
                };
                persistence.AttributeValues.Add(persistedValue);
            }

            persistedValue.Ordinal = ordinal++;
            AttributeValuePersistenceWriter.Write(domainValue, persistedValue);
        }

        foreach (var value in existing.Values.Where(value => !retainedIds.Contains(value.AttributeDefinitionId)))
            persistence.AttributeValues.Remove(value);
    }

    private static void SynchronizeVariantAttributeValues(
        ProductVariant domainVariant,
        ProductVariantPersistence persistedVariant)
    {
        var existing = persistedVariant.AttributeValues.ToDictionary(value => value.AttributeDefinitionId);
        var retainedIds = new HashSet<Guid>();
        var ordinal = 0;

        foreach (var domainValue in domainVariant.AttributeValues)
        {
            var definitionId = domainValue.AttributeDefinitionId.Value;
            retainedIds.Add(definitionId);

            if (!existing.TryGetValue(definitionId, out var persistedValue))
            {
                persistedValue = new ProductVariantAttributeValuePersistence
                {
                    ProductVariantId = persistedVariant.Id,
                    AttributeDefinitionId = definitionId,
                    ProductVariant = persistedVariant
                };
                persistedVariant.AttributeValues.Add(persistedValue);
            }

            persistedValue.Ordinal = ordinal++;
            AttributeValuePersistenceWriter.Write(domainValue, persistedValue);
        }

        foreach (var value in existing.Values.Where(value => !retainedIds.Contains(value.AttributeDefinitionId)))
            persistedVariant.AttributeValues.Remove(value);
    }

    private static IReadOnlyList<T> ValidateCollection<T>(
        ICollection<T>? values,
        Guid productId,
        string collectionName)
        where T : class
    {
        if (values is null)
            throw InvalidStructure(productId, $"The {collectionName} collection is null.");
        if (values.Any(value => value is null))
            throw InvalidStructure(productId, $"The {collectionName} collection contains a null entry.");

        return values.ToList();
    }

    private static void ValidateNonEmptyUnique<T>(
        IReadOnlyCollection<T> values,
        Func<T, Guid> idSelector,
        Guid productId,
        string identityName)
    {
        if (values.Any(value => idSelector(value) == Guid.Empty))
            throw InvalidStructure(productId, $"The {identityName} must not contain empty values.");
        if (values.GroupBy(idSelector).Any(group => group.Count() > 1))
            throw InvalidStructure(productId, $"The {identityName} must be unique.");
    }

    private static void ValidateUniqueOrdinals<T>(
        IReadOnlyCollection<T> values,
        Func<T, int> ordinalSelector,
        Guid productId,
        string ordinalName)
    {
        if (values.GroupBy(ordinalSelector).Any(group => group.Count() > 1))
            throw InvalidStructure(productId, $"The {ordinalName} must be unique.");
    }

    private static InvalidOperationException InvalidStructure(Guid productId, string message) =>
        new($"Invalid persisted Product '{productId}': {message}");
}
