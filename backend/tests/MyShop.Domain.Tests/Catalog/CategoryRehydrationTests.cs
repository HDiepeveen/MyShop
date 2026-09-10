using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class CategoryRehydrationTests
{
    [Fact]
    public void Rehydrate_PreservesRootIdentityAndState()
    {
        var id = CategoryId.New();

        var category = Category.Rehydrate(id, "Root", null);

        Assert.Equal(id, category.Id);
        Assert.Equal("Root", category.Name);
        Assert.Null(category.ParentCategoryId);
        Assert.True(category.IsRoot);
    }

    [Fact]
    public void Rehydrate_PreservesParentIdentityAndState()
    {
        var id = CategoryId.New();
        var parentId = CategoryId.New();

        var category = Category.Rehydrate(id, "Child", parentId);

        Assert.Equal(id, category.Id);
        Assert.Equal(parentId, category.ParentCategoryId);
        Assert.False(category.IsRoot);
    }

    [Fact]
    public void Rehydrate_WithNonNullDefaultParentId_Throws()
    {
        var parentId = (CategoryId?)default(CategoryId);

        Assert.Throws<ArgumentException>(() =>
            Category.Rehydrate(CategoryId.New(), "Category", parentId));
    }

    [Fact]
    public void Rehydrate_RejectsInvalidIdentityParentAndName()
    {
        var id = CategoryId.New();

        Assert.Throws<ArgumentException>(() => Category.Rehydrate(default, "Category", null));
        Assert.Throws<ArgumentException>(() => Category.Rehydrate(id, "Category", id));
        Assert.Throws<ArgumentException>(() => Category.Rehydrate(id, " ", null));
    }
}
