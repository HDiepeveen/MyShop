using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductCategoryTests
{
    [Fact]
    public void NewProduct_StartsWithoutCategories()
    {
        var product = CreateProduct();

        Assert.Empty(product.CategoryIds);
    }

    [Fact]
    public void AssignToCategory_AddsCategory()
    {
        var product = CreateProduct();
        var categoryId = CategoryId.New();

        product.AssignToCategory(categoryId);

        Assert.Equal([categoryId], product.CategoryIds);
    }

    [Fact]
    public void AssignToCategory_AllowsMultipleDistinctCategories()
    {
        var product = CreateProduct();
        var first = CategoryId.New();
        var second = CategoryId.New();

        product.AssignToCategory(first);
        product.AssignToCategory(second);

        Assert.Equal([first, second], product.CategoryIds);
    }

    [Fact]
    public void AssignToCategory_DuplicateIsIdempotent()
    {
        var product = CreateProduct();
        var categoryId = CategoryId.New();

        product.AssignToCategory(categoryId);
        product.AssignToCategory(categoryId);

        Assert.Equal([categoryId], product.CategoryIds);
    }

    [Fact]
    public void AssignToCategory_WithDefaultId_ThrowsAndPreservesCategories()
    {
        var product = CreateProduct();
        var existing = CategoryId.New();
        product.AssignToCategory(existing);

        var exception = Record.Exception(() => product.AssignToCategory(default));

        Assert.IsType<ArgumentException>(exception);
        Assert.Equal([existing], product.CategoryIds);
    }

    [Fact]
    public void RemoveFromCategory_RemovesAssignedCategory()
    {
        var product = CreateProduct();
        var categoryId = CategoryId.New();
        product.AssignToCategory(categoryId);

        product.RemoveFromCategory(categoryId);

        Assert.Empty(product.CategoryIds);
    }

    [Fact]
    public void RemoveFromCategory_PreservesOtherCategories()
    {
        var product = CreateProduct();
        var first = CategoryId.New();
        var second = CategoryId.New();
        product.AssignToCategory(first);
        product.AssignToCategory(second);

        product.RemoveFromCategory(first);

        Assert.Equal([second], product.CategoryIds);
    }

    [Fact]
    public void RemoveFromCategory_WhenUnassigned_ThrowsAndPreservesCategories()
    {
        var product = CreateProduct();
        var existing = CategoryId.New();
        product.AssignToCategory(existing);

        var exception = Record.Exception(() => product.RemoveFromCategory(CategoryId.New()));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal([existing], product.CategoryIds);
    }

    [Fact]
    public void RemoveFromCategory_WithDefaultId_ThrowsAndPreservesCategories()
    {
        var product = CreateProduct();
        var existing = CategoryId.New();
        product.AssignToCategory(existing);

        var exception = Record.Exception(() => product.RemoveFromCategory(default));

        Assert.IsType<ArgumentException>(exception);
        Assert.Equal([existing], product.CategoryIds);
    }

    [Fact]
    public void CategoryIds_CannotBeMutatedThroughExposedPublicApi()
    {
        var product = CreateProduct();
        dynamic exposedCategoryIds = product.CategoryIds;

        var exception = Record.Exception(() => exposedCategoryIds.Add(CategoryId.New()));

        Assert.NotNull(exception);
        Assert.Empty(product.CategoryIds);
    }

    private static Product CreateProduct() => Product.Create("Product", ProductTypeId.New(), "Standard");
}