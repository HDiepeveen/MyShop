using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductRehydrationTests
{
    [Fact]
    public void Rehydrate_PreservesCompleteProductStateWithoutGeneratingState()
    {
        var productId = ProductId.New();
        var productTypeId = ProductTypeId.New();
        var firstVariantId = ProductVariantId.New();
        var secondVariantId = ProductVariantId.New();
        var definitionId = AttributeDefinitionId.New();
        var variantDefinitionId = AttributeDefinitionId.New();
        var categoryIds = new[] { CategoryId.New(), CategoryId.New() };
        var productValue = TextAttributeValue.Create(definitionId, "Product value");
        var variantValue = IntegerAttributeValue.Create(variantDefinitionId, 42);
        var firstVariant = ProductVariant.Rehydrate(
            firstVariantId, "First", Sku.Create("FIRST"), [variantValue]);
        var secondVariant = ProductVariant.Rehydrate(
            secondVariantId, "Second", null, []);

        var product = Product.Rehydrate(
            productId,
            productTypeId,
            "Product",
            [firstVariant, secondVariant],
            categoryIds,
            [productValue]);

        Assert.Equal(productId, product.Id);
        Assert.Equal(productTypeId, product.ProductTypeId);
        Assert.Equal("Product", product.Name);
        Assert.Equal([firstVariantId, secondVariantId], product.Variants.Select(variant => variant.Id));
        Assert.Equal("FIRST", product.Variants.First().Sku!.Value);
        Assert.Equal(categoryIds, product.CategoryIds);
        Assert.Same(productValue, Assert.Single(product.AttributeValues));
        Assert.Same(variantValue, Assert.Single(product.Variants.First().AttributeValues));
    }

    [Fact]
    public void Rehydrate_PreservesVariantOrderAndDoesNotCreateAnInitialVariant()
    {
        var first = ProductVariant.Rehydrate(ProductVariantId.New(), "First", null, []);
        var second = ProductVariant.Rehydrate(ProductVariantId.New(), "Second", null, []);

        var product = Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", [second, first], [], []);

        Assert.Equal([second.Id, first.Id], product.Variants.Select(variant => variant.Id));
        Assert.Equal(2, product.Variants.Count);
    }

    [Fact]
    public void Rehydrate_WithZeroVariants_Throws()
    {
        var exception = Record.Exception(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", [], [], []));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Rehydrate_WithDuplicateVariantIds_Throws()
    {
        var id = ProductVariantId.New();
        var first = ProductVariant.Rehydrate(id, "First", null, []);
        var second = ProductVariant.Rehydrate(id, "Second", null, []);

        var exception = Record.Exception(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", [first, second], [], []));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Rehydrate_WithDuplicateSkusIncludingDifferentCasing_Throws()
    {
        var first = ProductVariant.Rehydrate(ProductVariantId.New(), "First", Sku.Create("abc"), []);
        var second = ProductVariant.Rehydrate(ProductVariantId.New(), "Second", Sku.Create("ABC"), []);

        var exception = Record.Exception(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", [first, second], [], []));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Rehydrate_WithDuplicateCategoryIds_Throws()
    {
        var categoryId = CategoryId.New();

        var exception = Record.Exception(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product",
            [ProductVariant.Rehydrate(ProductVariantId.New(), "Variant", null, [])],
            [categoryId, categoryId], []));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Rehydrate_WithDuplicateProductAttributeDefinitions_Throws()
    {
        var definitionId = AttributeDefinitionId.New();
        var values = new AttributeValue[]
        {
            TextAttributeValue.Create(definitionId, "First"),
            TextAttributeValue.Create(definitionId, "Second")
        };

        var exception = Record.Exception(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product",
            [ProductVariant.Rehydrate(ProductVariantId.New(), "Variant", null, [])], [], values));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Rehydrate_WithNullCollections_ThrowsArgumentNullException()
    {
        var variant = ProductVariant.Rehydrate(ProductVariantId.New(), "Variant", null, []);

        Assert.Throws<ArgumentNullException>(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", null!, [], []));
        Assert.Throws<ArgumentNullException>(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", [variant], null!, []));
        Assert.Throws<ArgumentNullException>(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", [variant], [], null!));
    }

    [Fact]
    public void Rehydrate_WithNullEntries_ThrowsWithoutReturningAggregate()
    {
        var variant = ProductVariant.Rehydrate(ProductVariantId.New(), "Variant", null, []);
        var productValues = new AttributeValue[] { null! };

        Assert.Throws<InvalidOperationException>(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", new[] { variant, null! }, [], []));
        Assert.Throws<InvalidOperationException>(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", [variant], [], productValues));
    }

    [Fact]
    public void Rehydrate_WithInvalidIdentity_ThrowsBeforeReturningAggregate()
    {
        var variant = ProductVariant.Rehydrate(ProductVariantId.New(), "Variant", null, []);

        Assert.Throws<ArgumentException>(() => Product.Rehydrate(
            default, ProductTypeId.New(), "Product", [variant], [], []));
        Assert.Throws<ArgumentException>(() => Product.Rehydrate(
            ProductId.New(), default, "Product", [variant], [], []));
        Assert.Throws<ArgumentException>(() => Product.Rehydrate(
            ProductId.New(), ProductTypeId.New(), "Product", [variant], [default], []));
    }
}

public sealed class ProductVariantRehydrationTests
{
    [Fact]
    public void Rehydrate_PreservesIdNameSkuAndAttributeValues()
    {
        var id = ProductVariantId.New();
        var value = DateAttributeValue.Create(AttributeDefinitionId.New(), new DateOnly(2026, 9, 10));

        var variant = ProductVariant.Rehydrate(id, "Variant", Sku.Create("ABC"), [value]);

        Assert.Equal(id, variant.Id);
        Assert.Equal("Variant", variant.Name);
        Assert.Equal("ABC", variant.Sku!.Value);
        Assert.Same(value, Assert.Single(variant.AttributeValues));
    }

    [Fact]
    public void Rehydrate_AllowsNullSku()
    {
        var variant = ProductVariant.Rehydrate(ProductVariantId.New(), "Variant", null, []);

        Assert.Null(variant.Sku);
        Assert.Empty(variant.AttributeValues);
    }

    [Fact]
    public void Rehydrate_PreservesAttributeValueOrder()
    {
        var first = IntegerAttributeValue.Create(AttributeDefinitionId.New(), 1);
        var second = IntegerAttributeValue.Create(AttributeDefinitionId.New(), 2);

        var variant = ProductVariant.Rehydrate(ProductVariantId.New(), "Variant", null, [first, second]);

        Assert.Equal([first, second], variant.AttributeValues);
    }

    [Fact]
    public void Rehydrate_WithNullCollectionOrEntry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ProductVariant.Rehydrate(
            ProductVariantId.New(), "Variant", null, null!));
        Assert.Throws<InvalidOperationException>(() => ProductVariant.Rehydrate(
            ProductVariantId.New(), "Variant", null, new AttributeValue[] { null! }));
    }

    [Fact]
    public void Rehydrate_WithDuplicateAttributeDefinitions_Throws()
    {
        var definitionId = AttributeDefinitionId.New();
        var values = new AttributeValue[]
        {
            IntegerAttributeValue.Create(definitionId, 1),
            IntegerAttributeValue.Create(definitionId, 2)
        };

        Assert.Throws<InvalidOperationException>(() => ProductVariant.Rehydrate(
            ProductVariantId.New(), "Variant", null, values));
    }

    [Fact]
    public void Rehydrate_WithDefaultIdOrInvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ProductVariant.Rehydrate(
            default, "Variant", null, []));
        Assert.Throws<ArgumentException>(() => ProductVariant.Rehydrate(
            ProductVariantId.New(), " ", null, []));
    }
}
