using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductVariantSkuTests
{
    [Fact]
    public void Create_InitialVariantStartsWithoutSku()
    {
        var product = CreateProduct();

        Assert.Null(Assert.Single(product.Variants).Sku);
    }

    [Fact]
    public void AddVariant_StartsWithoutSku()
    {
        var product = CreateProduct();

        var variant = product.AddVariant("Other");

        Assert.Null(variant.Sku);
    }

    [Fact]
    public void SetVariantSku_AssignsSkuToSelectedVariant()
    {
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var sku = Sku.Create("abc-123");

        product.SetVariantSku(variant.Id, sku);

        Assert.Equal(sku, variant.Sku);
    }

    [Fact]
    public void SetVariantSku_AssignedSkuIsVisibleThroughProductVariants()
    {
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);

        product.SetVariantSku(variant.Id, Sku.Create("ABC-123"));

        Assert.Equal("ABC-123", Assert.Single(product.Variants).Sku!.Value);
    }

    [Fact]
    public void SetVariantSku_OnOneVariant_DoesNotAffectAnother()
    {
        var product = CreateProduct();
        var first = Assert.Single(product.Variants);
        var second = product.AddVariant("Other");

        product.SetVariantSku(first.Id, Sku.Create("FIRST"));

        Assert.Equal("FIRST", first.Sku!.Value);
        Assert.Null(second.Sku);
    }

    [Fact]
    public void SetVariantSku_ChangesExistingSku()
    {
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        product.SetVariantSku(variant.Id, Sku.Create("OLD"));

        product.SetVariantSku(variant.Id, Sku.Create("NEW"));

        Assert.Equal("NEW", variant.Sku!.Value);
    }

    [Fact]
    public void SetVariantSku_WithCurrentSku_Succeeds()
    {
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        var sku = Sku.Create("ABC-123");
        product.SetVariantSku(variant.Id, sku);

        var exception = Record.Exception(() => product.SetVariantSku(variant.Id, sku));

        Assert.Null(exception);
        Assert.Equal(sku, variant.Sku);
    }

    [Fact]
    public void SetVariantSku_WithDifferentCasingTreatsSkuAsCurrentValue()
    {
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);
        product.SetVariantSku(variant.Id, Sku.Create("abc-123"));

        product.SetVariantSku(variant.Id, Sku.Create("ABC-123"));

        Assert.Equal("ABC-123", variant.Sku!.Value);
    }

    [Fact]
    public void SetVariantSku_WithDuplicateSkuOnAnotherVariant_Throws()
    {
        var product = CreateProduct();
        var first = Assert.Single(product.Variants);
        var second = product.AddVariant("Other");
        product.SetVariantSku(first.Id, Sku.Create("ABC-123"));

        var exception = Record.Exception(() => product.SetVariantSku(second.Id, Sku.Create("ABC-123")));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void SetVariantSku_WithCaseInsensitiveDuplicate_Throws()
    {
        var product = CreateProduct();
        var first = Assert.Single(product.Variants);
        var second = product.AddVariant("Other");
        product.SetVariantSku(first.Id, Sku.Create("abc-123"));

        var exception = Record.Exception(() => product.SetVariantSku(second.Id, Sku.Create("ABC-123")));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void SetVariantSku_FailedDuplicateAssignmentPreservesBothVariants()
    {
        var product = CreateProduct();
        var first = Assert.Single(product.Variants);
        var second = product.AddVariant("Other");
        product.SetVariantSku(first.Id, Sku.Create("FIRST"));
        product.SetVariantSku(second.Id, Sku.Create("SECOND"));

        var exception = Record.Exception(() => product.SetVariantSku(second.Id, Sku.Create("FIRST")));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("FIRST", first.Sku!.Value);
        Assert.Equal("SECOND", second.Sku!.Value);
    }

    [Fact]
    public void SetVariantSku_WithMissingVariant_Throws()
    {
        var product = CreateProduct();

        var exception = Record.Exception(() => product.SetVariantSku(
            ProductVariantId.New(), Sku.Create("ABC-123")));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void SetVariantSku_WithDefaultVariantId_Throws()
    {
        var product = CreateProduct();

        var exception = Record.Exception(() => product.SetVariantSku(
            default, Sku.Create("ABC-123")));

        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void SetVariantSku_WithNullSku_Throws()
    {
        var product = CreateProduct();
        var variant = Assert.Single(product.Variants);

        var exception = Record.Exception(() => product.SetVariantSku(variant.Id, null!));

        Assert.IsType<ArgumentNullException>(exception);
        Assert.Null(variant.Sku);
    }

    [Fact]
    public void Create_SignatureRemainsNameProductTypeAndInitialVariantName()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Standard");

        Assert.Equal("Product", product.Name);
        Assert.Equal("Standard", Assert.Single(product.Variants).Name);
    }

    [Fact]
    public void AddVariant_SignatureRemainsVariantName()
    {
        var product = CreateProduct();

        var variant = product.AddVariant("Other");

        Assert.Equal("Other", variant.Name);
        Assert.Null(variant.Sku);
    }

    [Fact]
    public void Sku_CannotBeAssignedThroughPublicSetter()
    {
        var property = typeof(ProductVariant).GetProperty(nameof(ProductVariant.Sku));

        Assert.NotNull(property);
        Assert.Null(property!.GetSetMethod());
    }

    private static Product CreateProduct() => Product.Create("Product", ProductTypeId.New(), "Standard");
}