using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class ProductPersistenceSynchronizerTests
{
    [Fact]
    public void Synchronize_RejectsNullArgumentsAndMismatchedIdentity()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var persistence = new ProductPersistence { Id = product.Id.Value };

        Assert.Throws<ArgumentNullException>(() =>
            ProductPersistenceSynchronizer.Synchronize(null!, persistence));
        Assert.Throws<ArgumentNullException>(() =>
            ProductPersistenceSynchronizer.Synchronize(product, null!));

        persistence.Id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() =>
            ProductPersistenceSynchronizer.Synchronize(product, persistence));

        persistence.Id = Guid.Empty;
        Assert.Throws<InvalidOperationException>(() =>
            ProductPersistenceSynchronizer.Synchronize(product, persistence));
    }

    [Fact]
    public void Synchronize_UpdatesCompleteGraphAndPreservesIdentityVersionAndOrder()
    {
        var scenario = CreateSynchronizationScenario();
        var originalVersion = scenario.Persistence.Version;

        ProductPersistenceSynchronizer.Synchronize(scenario.Product, scenario.Persistence);

        Assert.Equal(scenario.Product.Id.Value, scenario.Persistence.Id);
        Assert.Equal(scenario.Product.ProductTypeId.Value, scenario.Persistence.ProductTypeId);
        Assert.Equal(scenario.Product.Name, scenario.Persistence.Name);
        Assert.Equal(originalVersion, scenario.Persistence.Version);

        var variants = scenario.Persistence.Variants.OrderBy(variant => variant.Ordinal).ToArray();
        Assert.Equal([0, 1], variants.Select(variant => variant.Ordinal));
        Assert.Same(scenario.ExistingVariant, variants[0]);
        Assert.Equal("Primary", variants[0].Name);
        Assert.Equal("SKU-ONE", variants[0].Sku);
        Assert.Equal(scenario.Persistence.Id, variants[0].ProductId);
        Assert.DoesNotContain(scenario.RemovedVariant, scenario.Persistence.Variants);
        Assert.Equal(scenario.NewVariantId.Value, variants[1].Id);
        Assert.Equal(scenario.Persistence.Id, variants[1].ProductId);
        Assert.Same(scenario.Persistence, variants[1].Product);
        Assert.Equal("Secondary", variants[1].Name);
        Assert.Null(variants[1].Sku);

        var categories = scenario.Persistence.Categories.OrderBy(category => category.Ordinal).ToArray();
        Assert.Equal([0, 1], categories.Select(category => category.Ordinal));
        Assert.Same(scenario.ExistingCategory, categories[0]);
        Assert.DoesNotContain(scenario.RemovedCategory, scenario.Persistence.Categories);
        Assert.Equal(scenario.NewCategoryId.Value, categories[1].CategoryId);
        Assert.Equal(scenario.Persistence.Id, categories[1].ProductId);
        Assert.Same(scenario.Persistence, categories[1].Product);

        var productValues = scenario.Persistence.AttributeValues.OrderBy(value => value.Ordinal).ToArray();
        Assert.Equal([0, 1], productValues.Select(value => value.Ordinal));
        Assert.Same(scenario.ExistingProductValue, productValues[0]);
        Assert.Equal(AttributeDataType.Text, productValues[0].DataType);
        Assert.Equal("new product text", productValues[0].TextValue);
        Assert.Null(productValues[0].IntegerValue);
        Assert.Empty(productValues[0].MultiChoiceValues);
        Assert.DoesNotContain(scenario.RemovedProductValue, scenario.Persistence.AttributeValues);
        Assert.Equal(scenario.NewProductDefinitionId.Value, productValues[1].AttributeDefinitionId);
        Assert.Equal(12345m, productValues[1].DecimalCoefficient);
        Assert.Equal((byte)2, productValues[1].DecimalScale);
        Assert.Same(scenario.Persistence, productValues[1].Product);

        var primaryValues = variants[0].AttributeValues.OrderBy(value => value.Ordinal).ToArray();
        Assert.Equal([0, 1], primaryValues.Select(value => value.Ordinal));
        Assert.Same(scenario.ExistingVariantValue, primaryValues[0]);
        Assert.Equal(AttributeDataType.MultiChoice, primaryValues[0].DataType);
        Assert.Equal(["first", "second"], primaryValues[0].MultiChoiceValues
            .OrderBy(value => value.Ordinal).Select(value => value.Value));
        Assert.DoesNotContain(scenario.RemovedVariantValue, variants[0].AttributeValues);
        Assert.Equal(scenario.NewVariantDefinitionId.Value, primaryValues[1].AttributeDefinitionId);
        Assert.Equal("choice", primaryValues[1].ChoiceValue);
        Assert.Same(variants[0], primaryValues[1].ProductVariant);

        var secondaryValue = Assert.Single(variants[1].AttributeValues);
        Assert.Equal(0, secondaryValue.Ordinal);
        Assert.Equal(AttributeDataType.Boolean, secondaryValue.DataType);
        Assert.False(secondaryValue.BooleanValue);
        Assert.Same(variants[1], secondaryValue.ProductVariant);
    }

    [Theory]
    [InlineData(Corruption.EmptyVariantId)]
    [InlineData(Corruption.DuplicateVariantId)]
    [InlineData(Corruption.WrongVariantOwner)]
    [InlineData(Corruption.DuplicateVariantOrdinal)]
    [InlineData(Corruption.EmptyCategoryId)]
    [InlineData(Corruption.DuplicateCategoryId)]
    [InlineData(Corruption.WrongCategoryOwner)]
    [InlineData(Corruption.DuplicateCategoryOrdinal)]
    [InlineData(Corruption.EmptyProductDefinitionId)]
    [InlineData(Corruption.DuplicateProductDefinitionId)]
    [InlineData(Corruption.WrongProductAttributeOwner)]
    [InlineData(Corruption.DuplicateProductAttributeOrdinal)]
    [InlineData(Corruption.EmptyVariantDefinitionId)]
    [InlineData(Corruption.DuplicateVariantDefinitionId)]
    [InlineData(Corruption.WrongVariantAttributeOwner)]
    [InlineData(Corruption.DuplicateVariantAttributeOrdinal)]
    [InlineData(Corruption.WrongProductChildOwner)]
    [InlineData(Corruption.WrongProductChildDefinition)]
    [InlineData(Corruption.DuplicateProductChildOrdinal)]
    [InlineData(Corruption.WrongVariantChildOwner)]
    [InlineData(Corruption.WrongVariantChildDefinition)]
    [InlineData(Corruption.DuplicateVariantChildOrdinal)]
    [InlineData(Corruption.NullVariants)]
    [InlineData(Corruption.NullVariantEntry)]
    [InlineData(Corruption.NullCategories)]
    [InlineData(Corruption.NullCategoryEntry)]
    [InlineData(Corruption.NullProductAttributes)]
    [InlineData(Corruption.NullProductAttributeEntry)]
    [InlineData(Corruption.NullVariantAttributes)]
    [InlineData(Corruption.NullVariantAttributeEntry)]
    [InlineData(Corruption.NullProductChildren)]
    [InlineData(Corruption.NullProductChildEntry)]
    [InlineData(Corruption.NullVariantChildren)]
    [InlineData(Corruption.NullVariantChildEntry)]
    public void Synchronize_RejectsIndependentStructuralCorruption(Corruption corruption)
    {
        var graph = CreateValidationGraph();
        Corrupt(graph.Persistence, corruption);

        Assert.Throws<InvalidOperationException>(() =>
            ProductPersistenceSynchronizer.Synchronize(graph.Product, graph.Persistence));
    }

    [Fact]
    public void Synchronize_DeepStructuralFailureLeavesEntireGraphUnchanged()
    {
        var graph = CreateValidationGraph();
        var persistence = graph.Persistence;
        var variant = persistence.Variants.First();
        var variantValue = variant.AttributeValues.First();
        var child = variantValue.MultiChoiceValues.First();
        child.ProductVariantId = Guid.NewGuid();

        var originalProductTypeId = persistence.ProductTypeId;
        var originalName = persistence.Name;
        var originalVersion = persistence.Version;
        var originalVariants = persistence.Variants.ToArray();
        var originalCategories = persistence.Categories.ToArray();
        var originalProductValues = persistence.AttributeValues.ToArray();
        var originalVariantName = variant.Name;
        var originalVariantOrdinal = variant.Ordinal;
        var originalVariantValueOrdinal = variantValue.Ordinal;
        var originalVariantText = variantValue.TextValue;
        var originalChildValue = child.Value;

        Assert.NotEqual(graph.Product.Name, persistence.Name);
        Assert.Throws<InvalidOperationException>(() =>
            ProductPersistenceSynchronizer.Synchronize(graph.Product, persistence));

        Assert.Equal(originalProductTypeId, persistence.ProductTypeId);
        Assert.Equal(originalName, persistence.Name);
        Assert.Equal(originalVersion, persistence.Version);
        Assert.Equal(originalVariants, persistence.Variants);
        Assert.Equal(originalCategories, persistence.Categories);
        Assert.Equal(originalProductValues, persistence.AttributeValues);
        Assert.Equal(originalVariantName, variant.Name);
        Assert.Equal(originalVariantOrdinal, variant.Ordinal);
        Assert.Equal(originalVariantValueOrdinal, variantValue.Ordinal);
        Assert.Equal(originalVariantText, variantValue.TextValue);
        Assert.Equal(originalChildValue, child.Value);
    }

    private static SynchronizationScenario CreateSynchronizationScenario()
    {
        var product = Product.Create("New product", ProductTypeId.New(), "Primary");
        var primary = product.Variants.Single();
        product.SetVariantSku(primary.Id, Sku.Create("sku-one"));
        var second = product.AddVariant("Secondary");

        var existingCategoryId = CategoryId.New();
        var newCategoryId = CategoryId.New();
        product.AssignToCategory(existingCategoryId);
        product.AssignToCategory(newCategoryId);

        var existingProductDefinitionId = AttributeDefinitionId.New();
        var newProductDefinitionId = AttributeDefinitionId.New();
        product.SetAttributeValue(TextAttributeValue.Create(
            existingProductDefinitionId, "new product text"));
        product.SetAttributeValue(DecimalAttributeValue.Create(newProductDefinitionId, 123.4500m));

        var existingVariantDefinitionId = AttributeDefinitionId.New();
        var newVariantDefinitionId = AttributeDefinitionId.New();
        product.SetVariantAttributeValue(primary.Id, MultiChoiceAttributeValue.Create(
            existingVariantDefinitionId,
            [ChoiceValue.Create("first"), ChoiceValue.Create("second")]));
        product.SetVariantAttributeValue(primary.Id, ChoiceAttributeValue.Create(
            newVariantDefinitionId, ChoiceValue.Create("choice")));
        product.SetVariantAttributeValue(second.Id, BooleanAttributeValue.Create(
            AttributeDefinitionId.New(), false));

        var persistence = new ProductPersistence
        {
            Id = product.Id.Value,
            ProductTypeId = Guid.NewGuid(),
            Name = "Old product",
            Version = Guid.NewGuid()
        };

        var existingVariant = new ProductVariantPersistence
        {
            Id = primary.Id.Value,
            ProductId = persistence.Id,
            Product = persistence,
            Name = "Old primary",
            Sku = "OLD-SKU",
            Ordinal = 17
        };
        var removedVariant = new ProductVariantPersistence
        {
            Id = Guid.NewGuid(),
            ProductId = persistence.Id,
            Product = persistence,
            Name = "Removed",
            Ordinal = -9
        };
        persistence.Variants.Add(existingVariant);
        persistence.Variants.Add(removedVariant);

        var existingVariantValue = new ProductVariantAttributeValuePersistence
        {
            ProductVariantId = existingVariant.Id,
            AttributeDefinitionId = existingVariantDefinitionId.Value,
            ProductVariant = existingVariant,
            DataType = AttributeDataType.Text,
            TextValue = "stale",
            Ordinal = 22
        };
        var removedVariantValue = new ProductVariantAttributeValuePersistence
        {
            ProductVariantId = existingVariant.Id,
            AttributeDefinitionId = Guid.NewGuid(),
            ProductVariant = existingVariant,
            DataType = AttributeDataType.Integer,
            IntegerValue = 5,
            Ordinal = -6
        };
        existingVariant.AttributeValues.Add(existingVariantValue);
        existingVariant.AttributeValues.Add(removedVariantValue);

        var existingCategory = new ProductCategoryPersistence
        {
            ProductId = persistence.Id,
            CategoryId = existingCategoryId.Value,
            Product = persistence,
            Ordinal = 12
        };
        var removedCategory = new ProductCategoryPersistence
        {
            ProductId = persistence.Id,
            CategoryId = Guid.NewGuid(),
            Product = persistence,
            Ordinal = -3
        };
        persistence.Categories.Add(existingCategory);
        persistence.Categories.Add(removedCategory);

        var existingProductValue = new ProductAttributeValuePersistence
        {
            ProductId = persistence.Id,
            AttributeDefinitionId = existingProductDefinitionId.Value,
            Product = persistence,
            DataType = AttributeDataType.Integer,
            IntegerValue = 99,
            Ordinal = 14
        };
        existingProductValue.MultiChoiceValues.Add(ProductChild(existingProductValue, 5, "stale"));
        var removedProductValue = new ProductAttributeValuePersistence
        {
            ProductId = persistence.Id,
            AttributeDefinitionId = Guid.NewGuid(),
            Product = persistence,
            DataType = AttributeDataType.Boolean,
            BooleanValue = true,
            Ordinal = -4
        };
        persistence.AttributeValues.Add(existingProductValue);
        persistence.AttributeValues.Add(removedProductValue);

        return new SynchronizationScenario(
            product,
            persistence,
            existingVariant,
            removedVariant,
            second.Id,
            existingCategory,
            removedCategory,
            newCategoryId,
            existingProductValue,
            removedProductValue,
            newProductDefinitionId,
            existingVariantValue,
            removedVariantValue,
            newVariantDefinitionId);
    }

    private static ValidationGraph CreateValidationGraph()
    {
        var product = Product.Create("Domain product", ProductTypeId.New(), "Domain variant");
        var persistence = new ProductPersistence
        {
            Id = product.Id.Value,
            ProductTypeId = Guid.NewGuid(),
            Name = "Persisted product",
            Version = Guid.NewGuid()
        };

        var firstVariant = new ProductVariantPersistence
        {
            Id = product.Variants.Single().Id.Value,
            ProductId = persistence.Id,
            Product = persistence,
            Name = "Persisted variant",
            Ordinal = -8
        };
        var secondVariant = new ProductVariantPersistence
        {
            Id = Guid.NewGuid(),
            ProductId = persistence.Id,
            Product = persistence,
            Name = "Extra variant",
            Ordinal = 11
        };
        persistence.Variants.Add(firstVariant);
        persistence.Variants.Add(secondVariant);

        var firstVariantValue = VariantValue(firstVariant, Guid.NewGuid(), -5, true);
        var secondVariantValue = VariantValue(firstVariant, Guid.NewGuid(), 9, false);
        firstVariant.AttributeValues.Add(firstVariantValue);
        firstVariant.AttributeValues.Add(secondVariantValue);

        persistence.Categories.Add(Category(persistence, Guid.NewGuid(), -3));
        persistence.Categories.Add(Category(persistence, Guid.NewGuid(), 7));

        var firstProductValue = ProductValue(persistence, Guid.NewGuid(), -4, true);
        var secondProductValue = ProductValue(persistence, Guid.NewGuid(), 13, false);
        persistence.AttributeValues.Add(firstProductValue);
        persistence.AttributeValues.Add(secondProductValue);

        return new ValidationGraph(product, persistence);
    }

    private static void Corrupt(ProductPersistence persistence, Corruption corruption)
    {
        var firstVariant = persistence.Variants.ElementAt(0);
        var secondVariant = persistence.Variants.ElementAt(1);
        var firstCategory = persistence.Categories.ElementAt(0);
        var secondCategory = persistence.Categories.ElementAt(1);
        var firstProductValue = persistence.AttributeValues.ElementAt(0);
        var secondProductValue = persistence.AttributeValues.ElementAt(1);
        var firstVariantValue = firstVariant.AttributeValues.ElementAt(0);
        var secondVariantValue = firstVariant.AttributeValues.ElementAt(1);

        switch (corruption)
        {
            case Corruption.EmptyVariantId: secondVariant.Id = Guid.Empty; break;
            case Corruption.DuplicateVariantId: secondVariant.Id = firstVariant.Id; break;
            case Corruption.WrongVariantOwner: secondVariant.ProductId = Guid.NewGuid(); break;
            case Corruption.DuplicateVariantOrdinal: secondVariant.Ordinal = firstVariant.Ordinal; break;
            case Corruption.EmptyCategoryId: secondCategory.CategoryId = Guid.Empty; break;
            case Corruption.DuplicateCategoryId: secondCategory.CategoryId = firstCategory.CategoryId; break;
            case Corruption.WrongCategoryOwner: secondCategory.ProductId = Guid.NewGuid(); break;
            case Corruption.DuplicateCategoryOrdinal: secondCategory.Ordinal = firstCategory.Ordinal; break;
            case Corruption.EmptyProductDefinitionId: secondProductValue.AttributeDefinitionId = Guid.Empty; break;
            case Corruption.DuplicateProductDefinitionId:
                secondProductValue.AttributeDefinitionId = firstProductValue.AttributeDefinitionId; break;
            case Corruption.WrongProductAttributeOwner: secondProductValue.ProductId = Guid.NewGuid(); break;
            case Corruption.DuplicateProductAttributeOrdinal: secondProductValue.Ordinal = firstProductValue.Ordinal; break;
            case Corruption.EmptyVariantDefinitionId: secondVariantValue.AttributeDefinitionId = Guid.Empty; break;
            case Corruption.DuplicateVariantDefinitionId:
                secondVariantValue.AttributeDefinitionId = firstVariantValue.AttributeDefinitionId; break;
            case Corruption.WrongVariantAttributeOwner:
                secondVariantValue.ProductVariantId = Guid.NewGuid(); break;
            case Corruption.DuplicateVariantAttributeOrdinal:
                secondVariantValue.Ordinal = firstVariantValue.Ordinal; break;
            case Corruption.WrongProductChildOwner:
                firstProductValue.MultiChoiceValues.ElementAt(1).ProductId = Guid.NewGuid(); break;
            case Corruption.WrongProductChildDefinition:
                firstProductValue.MultiChoiceValues.ElementAt(1).AttributeDefinitionId = Guid.NewGuid(); break;
            case Corruption.DuplicateProductChildOrdinal:
                firstProductValue.MultiChoiceValues.ElementAt(1).Ordinal =
                    firstProductValue.MultiChoiceValues.ElementAt(0).Ordinal; break;
            case Corruption.WrongVariantChildOwner:
                firstVariantValue.MultiChoiceValues.ElementAt(1).ProductVariantId = Guid.NewGuid(); break;
            case Corruption.WrongVariantChildDefinition:
                firstVariantValue.MultiChoiceValues.ElementAt(1).AttributeDefinitionId = Guid.NewGuid(); break;
            case Corruption.DuplicateVariantChildOrdinal:
                firstVariantValue.MultiChoiceValues.ElementAt(1).Ordinal =
                    firstVariantValue.MultiChoiceValues.ElementAt(0).Ordinal; break;
            case Corruption.NullVariants: persistence.Variants = null!; break;
            case Corruption.NullVariantEntry: persistence.Variants.Add(null!); break;
            case Corruption.NullCategories: persistence.Categories = null!; break;
            case Corruption.NullCategoryEntry: persistence.Categories.Add(null!); break;
            case Corruption.NullProductAttributes: persistence.AttributeValues = null!; break;
            case Corruption.NullProductAttributeEntry: persistence.AttributeValues.Add(null!); break;
            case Corruption.NullVariantAttributes: firstVariant.AttributeValues = null!; break;
            case Corruption.NullVariantAttributeEntry: firstVariant.AttributeValues.Add(null!); break;
            case Corruption.NullProductChildren: firstProductValue.MultiChoiceValues = null!; break;
            case Corruption.NullProductChildEntry: firstProductValue.MultiChoiceValues.Add(null!); break;
            case Corruption.NullVariantChildren: firstVariantValue.MultiChoiceValues = null!; break;
            case Corruption.NullVariantChildEntry: firstVariantValue.MultiChoiceValues.Add(null!); break;
            default: throw new ArgumentOutOfRangeException(nameof(corruption), corruption, null);
        }
    }

    private static ProductCategoryPersistence Category(
        ProductPersistence product, Guid categoryId, int ordinal) => new()
    {
        ProductId = product.Id,
        CategoryId = categoryId,
        Ordinal = ordinal,
        Product = product
    };

    private static ProductAttributeValuePersistence ProductValue(
        ProductPersistence product, Guid definitionId, int ordinal, bool withChildren)
    {
        var value = new ProductAttributeValuePersistence
        {
            ProductId = product.Id,
            AttributeDefinitionId = definitionId,
            DataType = withChildren ? AttributeDataType.MultiChoice : AttributeDataType.Text,
            TextValue = withChildren ? null : "text",
            Ordinal = ordinal,
            Product = product
        };
        if (withChildren)
        {
            value.MultiChoiceValues.Add(ProductChild(value, -2, "first"));
            value.MultiChoiceValues.Add(ProductChild(value, 6, "second"));
        }
        return value;
    }

    private static ProductVariantAttributeValuePersistence VariantValue(
        ProductVariantPersistence variant, Guid definitionId, int ordinal, bool withChildren)
    {
        var value = new ProductVariantAttributeValuePersistence
        {
            ProductVariantId = variant.Id,
            AttributeDefinitionId = definitionId,
            DataType = withChildren ? AttributeDataType.MultiChoice : AttributeDataType.Text,
            TextValue = withChildren ? null : "text",
            Ordinal = ordinal,
            ProductVariant = variant
        };
        if (withChildren)
        {
            value.MultiChoiceValues.Add(VariantChild(value, -1, "first"));
            value.MultiChoiceValues.Add(VariantChild(value, 8, "second"));
        }
        return value;
    }

    private static ProductAttributeMultiChoiceValuePersistence ProductChild(
        ProductAttributeValuePersistence parent, int ordinal, string value) => new()
    {
        ProductId = parent.ProductId,
        AttributeDefinitionId = parent.AttributeDefinitionId,
        Ordinal = ordinal,
        Value = value,
        AttributeValue = parent
    };

    private static ProductVariantAttributeMultiChoiceValuePersistence VariantChild(
        ProductVariantAttributeValuePersistence parent, int ordinal, string value) => new()
    {
        ProductVariantId = parent.ProductVariantId,
        AttributeDefinitionId = parent.AttributeDefinitionId,
        Ordinal = ordinal,
        Value = value,
        AttributeValue = parent
    };

    public enum Corruption
    {
        EmptyVariantId,
        DuplicateVariantId,
        WrongVariantOwner,
        DuplicateVariantOrdinal,
        EmptyCategoryId,
        DuplicateCategoryId,
        WrongCategoryOwner,
        DuplicateCategoryOrdinal,
        EmptyProductDefinitionId,
        DuplicateProductDefinitionId,
        WrongProductAttributeOwner,
        DuplicateProductAttributeOrdinal,
        EmptyVariantDefinitionId,
        DuplicateVariantDefinitionId,
        WrongVariantAttributeOwner,
        DuplicateVariantAttributeOrdinal,
        WrongProductChildOwner,
        WrongProductChildDefinition,
        DuplicateProductChildOrdinal,
        WrongVariantChildOwner,
        WrongVariantChildDefinition,
        DuplicateVariantChildOrdinal,
        NullVariants,
        NullVariantEntry,
        NullCategories,
        NullCategoryEntry,
        NullProductAttributes,
        NullProductAttributeEntry,
        NullVariantAttributes,
        NullVariantAttributeEntry,
        NullProductChildren,
        NullProductChildEntry,
        NullVariantChildren,
        NullVariantChildEntry
    }

    private sealed record ValidationGraph(Product Product, ProductPersistence Persistence);

    private sealed record SynchronizationScenario(
        Product Product,
        ProductPersistence Persistence,
        ProductVariantPersistence ExistingVariant,
        ProductVariantPersistence RemovedVariant,
        ProductVariantId NewVariantId,
        ProductCategoryPersistence ExistingCategory,
        ProductCategoryPersistence RemovedCategory,
        CategoryId NewCategoryId,
        ProductAttributeValuePersistence ExistingProductValue,
        ProductAttributeValuePersistence RemovedProductValue,
        AttributeDefinitionId NewProductDefinitionId,
        ProductVariantAttributeValuePersistence ExistingVariantValue,
        ProductVariantAttributeValuePersistence RemovedVariantValue,
        AttributeDefinitionId NewVariantDefinitionId);
}
