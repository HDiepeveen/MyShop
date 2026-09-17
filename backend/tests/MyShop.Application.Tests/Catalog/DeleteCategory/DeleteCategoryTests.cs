using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.DeleteCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.DeleteCategory.DeleteCategory;

namespace MyShop.Application.Tests.Catalog.DeleteCategory;

public sealed class DeleteCategoryTests
{
    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var store = new StoreFake();
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, store, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_StopsBeforeRepositoryAccess()
    {
        var store = new StoreFake();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new UseCase(store, store, store).ExecuteAsync(
                CategoryId.New(), new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task ExecuteAsync_UnusedCategory_IsDeleted()
    {
        var store = new StoreFake { Category = Category.CreateRoot("Old") };
        var result = await new UseCase(store, store, store).ExecuteAsync(
            store.Category.Id, CancellationToken.None);
        Assert.Equal(DeleteCategoryOutcome.Succeeded, result.Outcome);
        Assert.Equal(store.Category.Id, store.DeletedId);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 2)]
    public async Task ExecuteAsync_InUseCategory_ReturnsConflictWithoutDelete(int children, int products)
    {
        var store = new StoreFake
        {
            Category = Category.CreateRoot("Used"),
            Usage = new CategoryUsage(children, products)
        };
        var result = await new UseCase(store, store, store).ExecuteAsync(
            store.Category.Id, CancellationToken.None);
        Assert.Equal(DeleteCategoryOutcome.InUse, result.Outcome);
        Assert.Null(store.DeletedId);
    }

    private sealed class StoreFake : ICategoryRepository, ICategoryUsageRepository, ICategoryWriter
    {
        public Category? Category { get; set; }
        public CategoryUsage Usage { get; set; } = new(0, 0);
        public CategoryId? DeletedId { get; private set; }
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token) => Task.FromResult(Category);
        public Task<CategoryUsage> GetUsageAsync(CategoryId id, CancellationToken token) => Task.FromResult(Usage);
        public Task AddAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteAsync(CategoryId categoryId, CancellationToken token)
        {
            DeletedId = categoryId;
            return Task.CompletedTask;
        }
    }
}
