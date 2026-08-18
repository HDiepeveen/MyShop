using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductAttributeValueTests
{
    [Fact]
    public void NewProductAndInitialVariant_StartWithoutAttributeValues()
    {
        // Arrange

        // Act
        var product = CreateProduct();

        // Assert
        Assert.Empty(product.AttributeValues);
        Assert.Empty(Assert.Single(product.Variants).AttributeValues);
    }

    [Fact]
    public void SetAttributeValue_AddsProductValue()
    {
        // Arrange
        var product = CreateProduct();
        var value = IntegerAttributeValue.Create(AttributeDefinitionId.New(), 2025);

        // Act
        product.SetAttributeValue(value);

        // Assert
        Assert.Same(value, Assert.Single(product.AttributeValues));
    }

    [Fact]
    public void SetAttributeValue_WithSameDefinition_ReplacesExistingValueAndType()
    {
        // Arrange
        var product = CreateProduct();
        var definitionId = AttributeDefinitionId.New();
        product.SetAttributeValue(IntegerAttributeValue.Create(definitionId, 2025));
        var replacement = TextAttributeValue.Create(definitionId, "2025");

        // Act
        product.SetAttributeValue(replacement);

        // Assert
        Assert.Same(replacement, Assert.Single(product.AttributeValues));
    }

    [Fact]
    public void SetAttributeValue_WithDifferentDefinitions_KeepsBothValues()
    {
        // Arrange
        var product = CreateProduct();

        // Act
        product.SetAttributeValue(TextAttributeValue.Create(AttributeDefinitionId.New(), "Author"));
        product.SetAttributeValue(IntegerAttributeValue.Create(AttributeDefinitionId.New(), 464));

        // Assert
        Assert.Equal(2, product.AttributeValues.Count);
    }

    [Fact]
    public void SetAttributeValue_WithNullValue_ThrowsAndPreservesValues()
    {
        // Arrange
        var product = CreateProduct();
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Author");
        product.SetAttributeValue(existing);

        // Act
        var exception = Record.Exception(() => product.SetAttributeValue(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
        Assert.Same(existing, Assert.Single(product.AttributeValues));
    }

    [Fact]
    public void RemoveAttributeValue_WithKnownDefinition_RemovesValue()
    {
        // Arrange
        var product = CreateProduct();
        var definitionId = AttributeDefinitionId.New();
        product.SetAttributeValue(TextAttributeValue.Create(definitionId, "Author"));

        // Act
        product.RemoveAttributeValue(definitionId);

        // Assert
        Assert.Empty(product.AttributeValues);
    }

    [Fact]
    public void RemoveAttributeValue_WithDefaultDefinitionId_ThrowsAndPreservesValues()
    {
        // Arrange
        var product = CreateProduct();
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Author");
        product.SetAttributeValue(existing);

        // Act
        var exception = Record.Exception(() => product.RemoveAttributeValue(default));

        // Assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Same(existing, Assert.Single(product.AttributeValues));
    }

    [Fact]
    public void RemoveAttributeValue_WithUnknownDefinitionId_ThrowsAndPreservesValues()
    {
        // Arrange
        var product = CreateProduct();
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Author");
        product.SetAttributeValue(existing);

        // Act
        var exception = Record.Exception(() => product.RemoveAttributeValue(AttributeDefinitionId.New()));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Same(existing, Assert.Single(product.AttributeValues));
    }

    [Fact]
    public void ProductAttributeValues_CannotBeMutatedThroughExposedPublicApi()
    {
        // Arrange
        var product = CreateProduct();
        dynamic exposedValues = product.AttributeValues;

        // Act
        var exception = Record.Exception(() => exposedValues.Add(
            TextAttributeValue.Create(AttributeDefinitionId.New(), "Author")));

        // Assert
        Assert.NotNull(exception);
        Assert.Empty(product.AttributeValues);
    }

    [Fact]
    public void SetVariantAttributeValue_AddsValueToTargetVariantOnly()
    {
        // Arrange
        var product = CreateProduct();
        var firstVariant = Assert.Single(product.Variants);
        var secondVariant = product.AddVariant("Second");
        var value = ChoiceAttributeValue.Create(
            AttributeDefinitionId.New(), ChoiceValue.Create("black"));

        // Act
        product.SetVariantAttributeValue(secondVariant.Id, value);

        // Assert
        Assert.Empty(firstVariant.AttributeValues);
        Assert.Same(value, Assert.Single(secondVariant.AttributeValues));
        Assert.Empty(product.AttributeValues);
    }

    [Fact]
    public void SetVariantAttributeValue_WithSameDefinition_ReplacesExistingValue()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var definitionId = AttributeDefinitionId.New();
        product.SetVariantAttributeValue(
            variant.Id,
            ChoiceAttributeValue.Create(definitionId, ChoiceValue.Create("black")));
        var replacement = ChoiceAttributeValue.Create(definitionId, ChoiceValue.Create("white"));

        // Act
        product.SetVariantAttributeValue(variant.Id, replacement);

        // Assert
        Assert.Same(replacement, Assert.Single(variant.AttributeValues));
    }

    [Fact]
    public void DifferentVariants_CanHaveDifferentValuesForSameDefinition()
    {
        // Arrange
        var product = CreateProduct();
        var firstVariant = Assert.Single(product.Variants);
        var secondVariant = product.AddVariant("Second");
        var definitionId = AttributeDefinitionId.New();

        // Act
        product.SetVariantAttributeValue(
            firstVariant.Id,
            ChoiceAttributeValue.Create(definitionId, ChoiceValue.Create("black")));
        product.SetVariantAttributeValue(
            secondVariant.Id,
            ChoiceAttributeValue.Create(definitionId, ChoiceValue.Create("white")));

        // Assert
        Assert.Equal("black", Assert.IsType<ChoiceAttributeValue>(Assert.Single(firstVariant.AttributeValues)).Value.Value);
        Assert.Equal("white", Assert.IsType<ChoiceAttributeValue>(Assert.Single(secondVariant.AttributeValues)).Value.Value);
    }

    [Fact]
    public void ProductAndVariant_CanHaveIndependentValuesForSameDefinition()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var definitionId = AttributeDefinitionId.New();

        // Act
        product.SetAttributeValue(TextAttributeValue.Create(definitionId, "product"));
        product.SetVariantAttributeValue(variant.Id, TextAttributeValue.Create(definitionId, "variant"));

        // Assert
        Assert.Equal("product", Assert.IsType<TextAttributeValue>(Assert.Single(product.AttributeValues)).Value);
        Assert.Equal("variant", Assert.IsType<TextAttributeValue>(Assert.Single(variant.AttributeValues)).Value);
    }

    [Fact]
    public void SetVariantAttributeValue_WithNullValue_ThrowsAndPreservesValues()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "existing");
        product.SetVariantAttributeValue(variant.Id, existing);

        // Act
        var exception = Record.Exception(() => product.SetVariantAttributeValue(variant.Id, null!));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
        Assert.Same(existing, Assert.Single(variant.AttributeValues));
    }

    [Fact]
    public void SetVariantAttributeValue_WithUnknownVariantId_ThrowsAndPreservesValues()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var value = TextAttributeValue.Create(AttributeDefinitionId.New(), "value");

        // Act
        var exception = Record.Exception(() =>
            product.SetVariantAttributeValue(ProductVariantId.New(), value));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Empty(variant.AttributeValues);
    }

    [Fact]
    public void RemoveVariantAttributeValue_WithKnownIds_RemovesValue()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var definitionId = AttributeDefinitionId.New();
        product.SetVariantAttributeValue(variant.Id, TextAttributeValue.Create(definitionId, "value"));

        // Act
        product.RemoveVariantAttributeValue(variant.Id, definitionId);

        // Assert
        Assert.Empty(variant.AttributeValues);
    }

    [Fact]
    public void RemoveVariantAttributeValue_WithDefaultDefinitionId_ThrowsAndPreservesValues()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "existing");
        product.SetVariantAttributeValue(variant.Id, existing);

        // Act
        var exception = Record.Exception(() =>
            product.RemoveVariantAttributeValue(variant.Id, default));

        // Assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Same(existing, Assert.Single(variant.AttributeValues));
    }

    [Fact]
    public void RemoveVariantAttributeValue_WithUnknownDefinitionId_ThrowsAndPreservesValues()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "existing");
        product.SetVariantAttributeValue(variant.Id, existing);

        // Act
        var exception = Record.Exception(() =>
            product.RemoveVariantAttributeValue(variant.Id, AttributeDefinitionId.New()));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Same(existing, Assert.Single(variant.AttributeValues));
    }

    [Fact]
    public void RemoveVariantAttributeValue_WithUnknownVariantId_ThrowsAndPreservesValues()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var definitionId = AttributeDefinitionId.New();
        var existing = TextAttributeValue.Create(definitionId, "existing");
        product.SetVariantAttributeValue(variant.Id, existing);

        // Act
        var exception = Record.Exception(() =>
            product.RemoveVariantAttributeValue(ProductVariantId.New(), definitionId));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Same(existing, Assert.Single(variant.AttributeValues));
    }

    [Fact]
    public void VariantAttributeValues_CannotBeMutatedThroughExposedPublicApi()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        dynamic exposedValues = variant.AttributeValues;

        // Act
        var exception = Record.Exception(() => exposedValues.Add(
            TextAttributeValue.Create(AttributeDefinitionId.New(), "value")));

        // Assert
        Assert.NotNull(exception);
        Assert.Empty(variant.AttributeValues);
    }

    private static Product CreateProduct() => Product.Create("Product", ProductTypeId.New(), "Standard");
}
