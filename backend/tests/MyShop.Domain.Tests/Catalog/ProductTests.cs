using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidValues_CreatesProductWithInitialVariant()
    {
        // Arrange
        var productTypeId = ProductTypeId.New();

        // Act
        var product = Product.Create("Clean Code", productTypeId, "Paperback");

        // Assert
        Assert.NotEqual(default, product.Id);
        Assert.NotEqual(Guid.Empty, product.Id.Value);
        Assert.Equal(productTypeId, product.ProductTypeId);
        Assert.Equal("Clean Code", product.Name);
        var variant = Assert.Single(product.Variants);
        Assert.NotEqual(default, variant.Id);
        Assert.NotEqual(Guid.Empty, variant.Id.Value);
        Assert.Equal("Paperback", variant.Name);
    }

    [Fact]
    public void Create_SeparateProductsAndVariantsHaveDistinctIds()
    {
        // Arrange
        var productTypeId = ProductTypeId.New();

        // Act
        var first = Product.Create("First", productTypeId, "Standard");
        var second = Product.Create("Second", productTypeId, "Standard");

        // Assert
        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(Assert.Single(first.Variants).Id, Assert.Single(second.Variants).Id);
    }

    [Fact]
    public void Create_WithDefaultProductTypeId_Throws()
    {
        // Arrange

        // Act
        var exception = Record.Exception(() => Product.Create("Clean Code", default, "Paperback"));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidProductName_Throws(string? name)
    {
        // Arrange
        var productTypeId = ProductTypeId.New();

        // Act
        var exception = Record.Exception(() => Product.Create(name!, productTypeId, "Standard"));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidInitialVariantName_Throws(string? name)
    {
        // Arrange
        var productTypeId = ProductTypeId.New();

        // Act
        var exception = Record.Exception(() => Product.Create("Product", productTypeId, name!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Fact]
    public void Rename_WithValidName_ChangesNameAndPreservesIdentityAndType()
    {
        // Arrange
        var productTypeId = ProductTypeId.New();
        var product = Product.Create("Clean Code", productTypeId, "Paperback");
        var originalId = product.Id;

        // Act
        product.Rename("Clean Code, Second Edition");

        // Assert
        Assert.Equal("Clean Code, Second Edition", product.Name);
        Assert.Equal(originalId, product.Id);
        Assert.Equal(productTypeId, product.ProductTypeId);
        Assert.Single(product.Variants);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rename_WithInvalidName_ThrowsAndPreservesName(string? name)
    {
        // Arrange
        var product = CreateProduct();

        // Act
        var exception = Record.Exception(() => product.Rename(name!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Equal("Product", product.Name);
    }

    [Fact]
    public void Variants_CannotBeMutatedThroughExposedPublicApi()
    {
        // Arrange
        var product = CreateProduct();
        var otherProduct = Product.Create("Other", ProductTypeId.New(), "Other variant");
        var foreignVariant = Assert.Single(otherProduct.Variants);
        dynamic exposedVariants = product.Variants;

        // Act
        var exception = Record.Exception(() => exposedVariants.Add(foreignVariant));

        // Assert
        Assert.NotNull(exception);
        Assert.Single(product.Variants);
    }

    [Fact]
    public void AddVariant_WithValidName_AddsVariantWithGeneratedDistinctId()
    {
        // Arrange
        var product = CreateProduct();
        var initialVariant = Assert.Single(product.Variants);

        // Act
        var addedVariant = product.AddVariant("Second variant");

        // Assert
        Assert.Equal(2, product.Variants.Count);
        Assert.Contains(addedVariant, product.Variants);
        Assert.NotEqual(default, addedVariant.Id);
        Assert.NotEqual(Guid.Empty, addedVariant.Id.Value);
        Assert.NotEqual(initialVariant.Id, addedVariant.Id);
        Assert.Equal("Second variant", addedVariant.Name);
    }

    [Fact]
    public void AddVariant_AllowsDuplicateNames()
    {
        // Arrange
        var product = Product.Create("Product", ProductTypeId.New(), "Standard");

        // Act
        product.AddVariant("Standard");

        // Assert
        Assert.Equal(2, product.Variants.Count);
        Assert.All(product.Variants, variant => Assert.Equal("Standard", variant.Name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddVariant_WithInvalidName_ThrowsAndPreservesCollection(string? name)
    {
        // Arrange
        var product = CreateProduct();
        var originalVariant = Assert.Single(product.Variants);

        // Act
        var exception = Record.Exception(() => product.AddVariant(name!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Same(originalVariant, Assert.Single(product.Variants));
    }

    [Fact]
    public void RenameVariant_WithKnownId_ChangesOnlySelectedVariantAndPreservesId()
    {
        // Arrange
        var product = CreateProduct();
        var initialVariant = Assert.Single(product.Variants);
        var otherVariant = product.AddVariant("Other");
        var originalId = otherVariant.Id;

        // Act
        product.RenameVariant(otherVariant.Id, "Renamed");

        // Assert
        Assert.Equal("Renamed", otherVariant.Name);
        Assert.Equal(originalId, otherVariant.Id);
        Assert.Equal("Standard", initialVariant.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RenameVariant_WithInvalidName_ThrowsAndPreservesName(string? name)
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);

        // Act
        var exception = Record.Exception(() => product.RenameVariant(variant.Id, name!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Equal("Standard", variant.Name);
    }

    [Fact]
    public void RenameVariant_WithUnknownId_ThrowsAndPreservesVariants()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);

        // Act
        var exception = Record.Exception(() => product.RenameVariant(ProductVariantId.New(), "Renamed"));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("Standard", variant.Name);
        Assert.Single(product.Variants);
    }

    [Fact]
    public void RemoveVariant_WhenAnotherVariantRemains_RemovesSelectedVariant()
    {
        // Arrange
        var product = CreateProduct();
        var initialVariant = Assert.Single(product.Variants);
        var removableVariant = product.AddVariant("Removable");

        // Act
        product.RemoveVariant(removableVariant.Id);

        // Assert
        Assert.Same(initialVariant, Assert.Single(product.Variants));
        Assert.DoesNotContain(removableVariant, product.Variants);
    }

    [Fact]
    public void RemoveVariant_WhenItIsFinalVariant_ThrowsAndPreservesVariant()
    {
        // Arrange
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);

        // Act
        var exception = Record.Exception(() => product.RemoveVariant(variant.Id));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Same(variant, Assert.Single(product.Variants));
    }

    [Fact]
    public void RemoveVariant_WithUnknownId_ThrowsAndPreservesVariants()
    {
        // Arrange
        var product = CreateProduct();
        var existingVariant = Assert.Single(product.Variants);

        // Act
        var exception = Record.Exception(() => product.RemoveVariant(ProductVariantId.New()));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Same(existingVariant, Assert.Single(product.Variants));
    }

    [Fact]
    public void GenericModel_SupportsBook()
    {
        // Arrange

        // Act
        var product = Product.Create("Clean Code", ProductTypeId.New(), "Paperback");

        // Assert
        Assert.Equal("Clean Code", product.Name);
        Assert.Equal("Paperback", Assert.Single(product.Variants).Name);
    }

    [Fact]
    public void GenericModel_SupportsClothing()
    {
        // Arrange
        var product = Product.Create("Basic T-Shirt", ProductTypeId.New(), "Black / S");

        // Act
        product.AddVariant("Black / M");
        product.AddVariant("White / S");
        product.AddVariant("White / M");

        // Assert
        Assert.Equal("Basic T-Shirt", product.Name);
        Assert.Equal(
            ["Black / S", "Black / M", "White / S", "White / M"],
            product.Variants.Select(variant => variant.Name));
    }

    [Fact]
    public void GenericModel_SupportsCar()
    {
        // Arrange

        // Act
        var product = Product.Create("Volvo XC40 Recharge", ProductTypeId.New(), "Ultimate AWD");

        // Assert
        Assert.Equal("Volvo XC40 Recharge", product.Name);
        Assert.Equal("Ultimate AWD", Assert.Single(product.Variants).Name);
    }

    private static Product CreateProduct() => Product.Create("Product", ProductTypeId.New(), "Standard");
}

public sealed class ProductIdentityTests
{
    [Fact]
    public void NewIds_AreNonEmptyAndDistinct()
    {
        // Arrange

        // Act
        var productId = ProductId.New();
        var otherProductId = ProductId.New();
        var variantId = ProductVariantId.New();
        var otherVariantId = ProductVariantId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, productId.Value);
        Assert.NotEqual(productId, otherProductId);
        Assert.NotEqual(Guid.Empty, variantId.Value);
        Assert.NotEqual(variantId, otherVariantId);
    }
}
