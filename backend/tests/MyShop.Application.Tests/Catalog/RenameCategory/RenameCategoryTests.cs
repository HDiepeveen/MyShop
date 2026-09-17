using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RenameCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameCategory.RenameCategory;

namespace MyShop.Application.Tests.Catalog.RenameCategory;

public sealed class RenameCategoryTests
{
    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var store = new StoreFake();
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_StopsBeforeRepositoryAccess()
    {
        var store = new StoreFake();
        var command = new RenameCategoryCommand(CategoryId.New(), "New");
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new UseCase(store, store).ExecuteAsync(command, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullCommandAndEmptyCategoryId()
    {
        var store = new StoreFake();
        var useCase = new UseCase(store, store);
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new RenameCategoryCommand(default, "New"), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_RenamesAndPersistsExistingCategory()
    {
        var store = new StoreFake { Category = Category.CreateRoot("Old") };
        Assert.True(await new UseCase(store, store).ExecuteAsync(
            new(store.Category.Id, "New"), CancellationToken.None));
        Assert.Equal("New", store.Category.Name);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task ExecuteAsync_UnchangedName_DoesNotPersist()
    {
        var store = new StoreFake { Category = Category.CreateRoot("Same") };
        Assert.True(await new UseCase(store, store).ExecuteAsync(
            new(store.Category.Id, "Same"), CancellationToken.None));
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task ExecuteAsync_MissingCategory_ReturnsFalse()
    {
        var store = new StoreFake();
        Assert.False(await new UseCase(store, store).ExecuteAsync(
            new(CategoryId.New(), "New"), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_InvalidName_DoesNotPersist()
    {
        var store = new StoreFake { Category = Category.CreateRoot("Original") };
        await Assert.ThrowsAsync<ArgumentException>(() => new UseCase(store, store).ExecuteAsync(
            new(store.Category.Id, ""), CancellationToken.None));
        Assert.Equal("Original", store.Category.Name);
        Assert.Equal(0, store.Saves);
    }

    private sealed class StoreFake : ICategoryRepository, ICategoryWriter
    {
        public Category? Category { get; set; }
        public int Saves { get; private set; }
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token) => Task.FromResult(Category);
        public Task AddAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(Category category, CancellationToken token) { Saves++; return Task.CompletedTask; }
        public Task DeleteAsync(CategoryId categoryId, CancellationToken token) => throw new NotSupportedException();
    }
}
