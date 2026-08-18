using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class CategoryTests
{
    [Fact]
    public void CreateRoot_WithValidName_CreatesRootWithGeneratedId()
    {
        // Arrange
        const string name = "Vehicles";

        // Act
        var category = Category.CreateRoot(name);

        // Assert
        Assert.NotEqual(default, category.Id);
        Assert.NotEqual(Guid.Empty, category.Id.Value);
        Assert.Equal(name, category.Name);
        Assert.Null(category.ParentCategoryId);
        Assert.True(category.IsRoot);
    }

    [Fact]
    public void CreateChild_WithValidParent_CreatesChildWithGeneratedId()
    {
        // Arrange
        var parent = Category.CreateRoot("Vehicles");

        // Act
        var category = Category.CreateChild("Cars", parent.Id);

        // Assert
        Assert.NotEqual(default, category.Id);
        Assert.NotEqual(Guid.Empty, category.Id.Value);
        Assert.Equal("Cars", category.Name);
        Assert.Equal(parent.Id, category.ParentCategoryId);
        Assert.False(category.IsRoot);
    }

    [Fact]
    public void Create_CreatesDistinctIds()
    {
        // Arrange

        // Act
        var first = Category.CreateRoot("Vehicles");
        var second = Category.CreateRoot("Books");

        // Assert
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void CreateChild_WithDefaultParentId_Throws()
    {
        // Arrange

        // Act
        var exception = Record.Exception(() => Category.CreateChild("Cars", default));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateRoot_WithInvalidName_Throws(string? name)
    {
        // Arrange

        // Act
        var exception = Record.Exception(() => Category.CreateRoot(name!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateChild_WithInvalidName_Throws(string? name)
    {
        // Arrange
        var parentId = CategoryId.New();

        // Act
        var exception = Record.Exception(() => Category.CreateChild(name!, parentId));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Fact]
    public void Rename_WithValidName_ChangesNameAndPreservesOtherState()
    {
        // Arrange
        var parentId = CategoryId.New();
        var category = Category.CreateChild("Cars", parentId);
        var originalId = category.Id;

        // Act
        category.Rename("Automobiles");

        // Assert
        Assert.Equal("Automobiles", category.Name);
        Assert.Equal(originalId, category.Id);
        Assert.Equal(parentId, category.ParentCategoryId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rename_WithInvalidName_ThrowsAndPreservesName(string? name)
    {
        // Arrange
        var category = Category.CreateRoot("Vehicles");

        // Act
        var exception = Record.Exception(() => category.Rename(name!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Equal("Vehicles", category.Name);
    }

    [Fact]
    public void MoveUnder_FromRoot_MakesCategoryAChildAndPreservesIdentityAndName()
    {
        // Arrange
        var category = Category.CreateRoot("Cars");
        var originalId = category.Id;
        var parentId = CategoryId.New();

        // Act
        category.MoveUnder(parentId);

        // Assert
        Assert.Equal(originalId, category.Id);
        Assert.Equal("Cars", category.Name);
        Assert.Equal(parentId, category.ParentCategoryId);
        Assert.False(category.IsRoot);
    }

    [Fact]
    public void MoveUnder_FromChild_ChangesParent()
    {
        // Arrange
        var category = Category.CreateChild("Cars", CategoryId.New());
        var newParentId = CategoryId.New();

        // Act
        category.MoveUnder(newParentId);

        // Assert
        Assert.Equal(newParentId, category.ParentCategoryId);
    }

    [Fact]
    public void MoveToRoot_FromChild_RemovesParentAndPreservesIdentityAndName()
    {
        // Arrange
        var category = Category.CreateChild("Cars", CategoryId.New());
        var originalId = category.Id;

        // Act
        category.MoveToRoot();

        // Assert
        Assert.Equal(originalId, category.Id);
        Assert.Equal("Cars", category.Name);
        Assert.Null(category.ParentCategoryId);
        Assert.True(category.IsRoot);
    }

    [Fact]
    public void MoveUnder_WithDefaultParentId_ThrowsAndPreservesParent()
    {
        // Arrange
        var originalParentId = CategoryId.New();
        var category = Category.CreateChild("Cars", originalParentId);

        // Act
        var exception = Record.Exception(() => category.MoveUnder(default));

        // Assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal(originalParentId, category.ParentCategoryId);
    }

    [Fact]
    public void MoveUnder_WithOwnId_ThrowsArgumentExceptionAndPreservesParent()
    {
        // Arrange
        var originalParentId = CategoryId.New();
        var category = Category.CreateChild("Cars", originalParentId);

        // Act
        var exception = Record.Exception(() => category.MoveUnder(category.Id));

        // Assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal(originalParentId, category.ParentCategoryId);
    }

    [Fact]
    public void MoveUnder_WithExistingParent_IsHarmless()
    {
        // Arrange
        var parentId = CategoryId.New();
        var category = Category.CreateChild("Cars", parentId);

        // Act
        category.MoveUnder(parentId);

        // Assert
        Assert.Equal(parentId, category.ParentCategoryId);
        Assert.False(category.IsRoot);
    }

    [Fact]
    public void MoveToRoot_WhenAlreadyRoot_IsHarmless()
    {
        // Arrange
        var category = Category.CreateRoot("Vehicles");

        // Act
        category.MoveToRoot();

        // Assert
        Assert.Null(category.ParentCategoryId);
        Assert.True(category.IsRoot);
    }
}
