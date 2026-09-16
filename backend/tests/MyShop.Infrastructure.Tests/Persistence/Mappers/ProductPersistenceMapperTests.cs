using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class ProductPersistenceMapperTests
{
    [Fact]
    public void ToSnapshot_ReconstructsCompleteOrderedGraphAndExactToken()
    {
        var persistence = CreateCompleteProduct();

        var snapshot = ProductPersistenceMapper.ToSnapshot(persistence);

        Assert.Equal(persistence.Id, snapshot.Product.Id.Value);
        Assert.Equal(persistence.ProductTypeId, snapshot.Product.ProductTypeId.Value);
        Assert.Equal("Product", snapshot.Product.Name);
        Assert.Equal(snapshot.Product.Id, snapshot.ConcurrencyToken.ProductId);
        Assert.Equal(persistence.Version, snapshot.ConcurrencyToken.Revision);

        var variants = snapshot.Product.Variants.ToArray();
        Assert.Equal(["First", "Second"], variants.Select(variant => variant.Name));
        Assert.Equal("ABC", variants[0].Sku!.Value);
        Assert.Null(variants[1].Sku);
        Assert.Equal([42L, 7L], variants[0].AttributeValues
            .Cast<IntegerAttributeValue>().Select(value => value.Value));

        Assert.Equal(
            persistence.Categories.OrderBy(category => category.Ordinal).Select(category => category.CategoryId),
            snapshot.Product.CategoryIds.Select(categoryId => categoryId.Value));
        Assert.Equal(["First value", "Second value"], snapshot.Product.AttributeValues
            .Cast<TextAttributeValue>().Select(value => value.Value));
        var multiChoice = Assert.IsType<MultiChoiceAttributeValue>(
            Assert.Single(variants[1].AttributeValues));
        Assert.Equal(["black", "white"], multiChoice.Values.Select(value => value.Value));

        var secondSnapshot = ProductPersistenceMapper.ToSnapshot(persistence);
        Assert.Equal(persistence.Version, secondSnapshot.ConcurrencyToken.Revision);
    }

    [Fact]
    public void ToSnapshot_AcceptsNegativeGappedAndNonzeroStartingOrdinals()
    {
        var persistence = CreateCompleteProduct();

        var snapshot = ProductPersistenceMapper.ToSnapshot(persistence);

        Assert.Equal(["First", "Second"], snapshot.Product.Variants.Select(variant => variant.Name));
        Assert.Equal(["First value", "Second value"], snapshot.Product.AttributeValues
            .Cast<TextAttributeValue>().Select(value => value.Value));
        Assert.Equal([42L, 7L], snapshot.Product.Variants.First().AttributeValues
            .Cast<IntegerAttributeValue>().Select(value => value.Value));
        Assert.Equal(
            persistence.Categories.OrderBy(category => category.Ordinal).Select(category => category.CategoryId),
            snapshot.Product.CategoryIds.Select(categoryId => categoryId.Value));
    }

    [Fact]
    public void ToSnapshot_RejectsDuplicateOrdinalInEveryOrderedCollection()
    {
        var product = CreateCompleteProduct();
        product.Variants.ElementAt(1).Ordinal = product.Variants.ElementAt(0).Ordinal;
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = CreateCompleteProduct();
        product.Categories.ElementAt(1).Ordinal = product.Categories.ElementAt(0).Ordinal;
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = CreateCompleteProduct();
        product.AttributeValues.ElementAt(1).Ordinal = product.AttributeValues.ElementAt(0).Ordinal;
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = CreateCompleteProduct();
        var variant = product.Variants.Single(candidate => candidate.Name == "First");
        variant.AttributeValues.ElementAt(1).Ordinal = variant.AttributeValues.ElementAt(0).Ordinal;
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
    }

    [Fact]
    public void ToSnapshot_RejectsNullArgumentCollectionsAndEntries()
    {
        Assert.Throws<ArgumentNullException>(() => ProductPersistenceMapper.ToSnapshot(null!));

        var product = ValidProduct(); product.Variants = null!;
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Categories = null!;
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.AttributeValues = null!;
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Variants.Single().AttributeValues = null!;
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = ValidProduct(); product.Variants.Add(null!);
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Categories.Add(null!);
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.AttributeValues.Add(null!);
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Variants.Single().AttributeValues.Add(null!);
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
    }

    [Fact]
    public void ToSnapshot_RejectsRelationalOwnerMismatches()
    {
        var product = ValidProduct(); product.Variants.Single().ProductId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = ValidProduct();
        product.Categories.Add(new ProductCategoryPersistence
        {
            ProductId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Ordinal = 1
        });
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = ValidProduct();
        product.AttributeValues.Add(ProductText(product, Guid.NewGuid(), 1, "value"));
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = ValidProduct();
        var variant = product.Variants.Single();
        variant.AttributeValues.Add(VariantInteger(variant, Guid.NewGuid(), 1, 1));
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
    }

    [Fact]
    public void ToSnapshot_DelegatesRootIdentityVersionAndAggregateValidation()
    {
        var product = ValidProduct(); product.Id = Guid.Empty;
        Assert.Throws<ArgumentException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.ProductTypeId = Guid.Empty;
        Assert.Throws<ArgumentException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Version = Guid.Empty;
        Assert.Throws<ArgumentException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Name = " ";
        Assert.Throws<ArgumentException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Variants.Clear();
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
    }

    [Fact]
    public void ToSnapshot_DelegatesVariantCategoryAndSkuValidation()
    {
        var product = ValidProduct(); product.Variants.Single().Id = Guid.Empty;
        Assert.Throws<ArgumentException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Variants.Single().Name = " ";
        Assert.Throws<ArgumentException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct(); product.Variants.Single().Sku = "invalid sku";
        Assert.Throws<ArgumentException>(() => ProductPersistenceMapper.ToSnapshot(product));
        product = ValidProduct();
        product.Categories.Add(new ProductCategoryPersistence
        {
            ProductId = product.Id, CategoryId = Guid.Empty, Ordinal = 1
        });
        Assert.Throws<ArgumentException>(() => ProductPersistenceMapper.ToSnapshot(product));
    }

    [Fact]
    public void ToSnapshot_DelegatesDuplicateDomainIdentityValidation()
    {
        var product = ValidProduct();
        var duplicateVariant = Variant(product, "Other", 2);
        duplicateVariant.Id = product.Variants.Single().Id;
        product.Variants.Add(duplicateVariant);
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = ValidProduct();
        var categoryId = Guid.NewGuid();
        product.Categories.Add(new ProductCategoryPersistence { ProductId = product.Id, CategoryId = categoryId, Ordinal = 1 });
        product.Categories.Add(new ProductCategoryPersistence { ProductId = product.Id, CategoryId = categoryId, Ordinal = 2 });
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = ValidProduct();
        var definitionId = Guid.NewGuid();
        product.AttributeValues.Add(ProductText(product, product.Id, 1, "first", definitionId));
        product.AttributeValues.Add(ProductText(product, product.Id, 2, "second", definitionId));
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = ValidProduct();
        var variant = product.Variants.Single();
        definitionId = Guid.NewGuid();
        variant.AttributeValues.Add(VariantInteger(variant, variant.Id, 1, 1, definitionId));
        variant.AttributeValues.Add(VariantInteger(variant, variant.Id, 2, 2, definitionId));
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));

        product = ValidProduct();
        product.Variants.Single().Sku = "abc";
        duplicateVariant = Variant(product, "Other", 2); duplicateVariant.Sku = "ABC";
        product.Variants.Add(duplicateVariant);
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(product));
    }

    private static ProductPersistence CreateCompleteProduct()
    {
        var product = new ProductPersistence
        {
            Id = Guid.NewGuid(), ProductTypeId = Guid.NewGuid(), Name = "Product", Version = Guid.NewGuid()
        };
        var first = Variant(product, "First", -10); first.Sku = "abc";
        first.AttributeValues.Add(VariantInteger(first, first.Id, 15, 7));
        first.AttributeValues.Add(VariantInteger(first, first.Id, -2, 42));
        var second = Variant(product, "Second", 20);
        var multi = new ProductVariantAttributeValuePersistence
        {
            ProductVariantId = second.Id,
            AttributeDefinitionId = Guid.NewGuid(),
            DataType = AttributeDataType.MultiChoice,
            Ordinal = 6
        };
        multi.MultiChoiceValues.Add(new ProductVariantAttributeMultiChoiceValuePersistence
        {
            ProductVariantId = second.Id, AttributeDefinitionId = multi.AttributeDefinitionId,
            Ordinal = 8, Value = "white"
        });
        multi.MultiChoiceValues.Add(new ProductVariantAttributeMultiChoiceValuePersistence
        {
            ProductVariantId = second.Id, AttributeDefinitionId = multi.AttributeDefinitionId,
            Ordinal = -9, Value = "black"
        });
        second.AttributeValues.Add(multi);
        product.Variants.Add(second);
        product.Variants.Add(first);

        product.Categories.Add(new ProductCategoryPersistence
        {
            ProductId = product.Id, CategoryId = Guid.NewGuid(), Ordinal = 9
        });
        product.Categories.Add(new ProductCategoryPersistence
        {
            ProductId = product.Id, CategoryId = Guid.NewGuid(), Ordinal = -3
        });
        product.AttributeValues.Add(ProductText(product, product.Id, 11, "Second value"));
        product.AttributeValues.Add(ProductText(product, product.Id, -7, "First value"));
        return product;
    }

    private static ProductPersistence ValidProduct()
    {
        var product = new ProductPersistence
        {
            Id = Guid.NewGuid(), ProductTypeId = Guid.NewGuid(), Name = "Product", Version = Guid.NewGuid()
        };
        product.Variants.Add(Variant(product, "Variant", 1));
        return product;
    }

    private static ProductVariantPersistence Variant(ProductPersistence product, string name, int ordinal) => new()
    {
        Id = Guid.NewGuid(), ProductId = product.Id, Name = name, Ordinal = ordinal
    };

    private static ProductAttributeValuePersistence ProductText(
        ProductPersistence product, Guid productId, int ordinal, string text, Guid? definitionId = null) => new()
    {
        ProductId = productId,
        AttributeDefinitionId = definitionId ?? Guid.NewGuid(),
        DataType = AttributeDataType.Text,
        Ordinal = ordinal,
        TextValue = text
    };

    private static ProductVariantAttributeValuePersistence VariantInteger(
        ProductVariantPersistence variant, Guid variantId, int ordinal, long value, Guid? definitionId = null) => new()
    {
        ProductVariantId = variantId,
        AttributeDefinitionId = definitionId ?? Guid.NewGuid(),
        DataType = AttributeDataType.Integer,
        Ordinal = ordinal,
        IntegerValue = value
    };
}
