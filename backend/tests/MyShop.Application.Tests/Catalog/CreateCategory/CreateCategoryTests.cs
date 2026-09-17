using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.CreateCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateCategory.CreateCategory;

namespace MyShop.Application.Tests.Catalog.CreateCategory;

public sealed class CreateCategoryTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesRootWithoutParentLookup()
    {
        var store = new StoreFake();
        var result = await new UseCase(store, store).ExecuteAsync(new("Clothing"), CancellationToken.None);
        Assert.NotNull(result.Category);
        Assert.True(result.Category.IsRoot);
        Assert.Same(result.Category, store.Added);
        Assert.Equal(0, store.LookupCount);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesChildWhenParentExists()
    {
        var store = new StoreFake { Found = Category.CreateRoot("Clothing") };
        var result = await new UseCase(store, store).ExecuteAsync(
            new("Shirts", store.Found.Id), CancellationToken.None);
        Assert.Equal(store.Found.Id, result.Category!.ParentCategoryId);
        Assert.Same(result.Category, store.Added);
    }

    [Fact]
    public async Task ExecuteAsync_MissingParent_DoesNotWrite()
    {
        var store = new StoreFake();
        var result = await new UseCase(store, store).ExecuteAsync(
            new("Shirts", CategoryId.New()), CancellationToken.None);
        Assert.True(result.ParentNotFound);
        Assert.Null(store.Added);
    }

    private sealed class StoreFake : ICategoryRepository, ICategoryWriter
    {
        public Category? Found { get; set; }
        public Category? Added { get; private set; }
        public int LookupCount { get; private set; }
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token)
        {
            LookupCount++;
            return Task.FromResult(Found);
        }
        public Task AddAsync(Category category, CancellationToken token)
        {
            Added = category;
            return Task.CompletedTask;
        }
        public Task SaveAsync(Category category, CancellationToken token) => throw new NotSupportedException();
    }
}
