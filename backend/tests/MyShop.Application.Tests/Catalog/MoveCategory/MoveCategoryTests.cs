using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.MoveCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.MoveCategory.MoveCategory;

namespace MyShop.Application.Tests.Catalog.MoveCategory;

public sealed class MoveCategoryTests
{
    [Fact]
    public async Task ExecuteAsync_MovesCategoryUnderValidParent()
    {
        var category = Category.CreateRoot("Shirts");
        var parent = Category.CreateRoot("Clothing");
        var store = new StoreFake(category, parent);
        var result = await new UseCase(store, store, store).ExecuteAsync(
            new(category.Id, parent.Id), CancellationToken.None);
        Assert.Equal(MoveCategoryResult.Succeeded, result);
        Assert.Equal(parent.Id, category.ParentCategoryId);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task ExecuteAsync_MovesChildToRoot()
    {
        var category = Category.CreateChild("Shirts", CategoryId.New());
        var store = new StoreFake(category);
        var result = await new UseCase(store, store, store).ExecuteAsync(
            new(category.Id, null), CancellationToken.None);
        Assert.Equal(MoveCategoryResult.Succeeded, result);
        Assert.True(category.IsRoot);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsDescendantParentWithoutWrite()
    {
        var category = Category.CreateRoot("Clothing");
        var child = Category.CreateChild("Shirts", category.Id);
        var store = new StoreFake(category, child) { IsDescendant = true };
        var result = await new UseCase(store, store, store).ExecuteAsync(
            new(category.Id, child.Id), CancellationToken.None);
        Assert.Equal(MoveCategoryResult.CycleDetected, result);
        Assert.Equal(0, store.Saves);
    }

    private sealed class StoreFake(params Category[] categories)
        : ICategoryRepository, ICategoryHierarchyRepository, ICategoryWriter
    {
        public bool IsDescendant { get; set; }
        public int Saves { get; private set; }
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token) =>
            Task.FromResult(categories.SingleOrDefault(category => category.Id == id));
        public Task<bool> IsDescendantOfAsync(CategoryId candidateId, CategoryId ancestorId, CancellationToken token) =>
            Task.FromResult(IsDescendant);
        public Task AddAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(Category category, CancellationToken token) { Saves++; return Task.CompletedTask; }
    }
}
