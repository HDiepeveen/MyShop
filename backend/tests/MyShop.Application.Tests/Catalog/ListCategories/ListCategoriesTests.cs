using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ListCategories;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListCategories.ListCategories;

namespace MyShop.Application.Tests.Catalog.ListCategories;

public sealed class ListCategoriesTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsRepositoryResultsAndForwardsToken()
    {
        var repository = new CategoryListRepositoryFake
        {
            Categories =
            [
                new CategoryListItem(Guid.NewGuid(), "Clothing", null, 2),
                new CategoryListItem(Guid.NewGuid(), "Shirts", Guid.NewGuid(), 0)
            ]
        };
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();

        var parentId = CategoryId.New();
        var result = await useCase.ExecuteAsync(
            new ListCategoriesQuery("  shirt  ", parentId), source.Token);

        Assert.Same(repository.Categories, result);
        Assert.Equal(source.Token, repository.Token);
        Assert.Equal(1, repository.ListCalls);
        Assert.Equal("shirt", repository.SearchTerm);
        Assert.Equal(parentId, repository.ParentCategoryId);
        Assert.False(repository.RootsOnly);
        Assert.Equal(0, repository.Offset);
        Assert.Equal(50, repository.Limit);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepository()
    {
        var repository = new CategoryListRepositoryFake();
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(new ListCategoriesQuery(), source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesRepositoryException()
    {
        var repository = new CategoryListRepositoryFake();
        var expected = new InvalidOperationException("failure");
        repository.Exception = expected;
        var useCase = new UseCase(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new ListCategoriesQuery(), CancellationToken.None));

        Assert.Same(expected, exception);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExecuteAsync_RejectsEmptySearchBeforeRepositoryAccess(string search)
    {
        var repository = new CategoryListRepositoryFake();
        var useCase = new UseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new ListCategoriesQuery(search), CancellationToken.None));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsEmptyParentIdBeforeRepositoryAccess()
    {
        var repository = new CategoryListRepositoryFake();
        var useCase = new UseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new ListCategoriesQuery(ParentCategoryId: (CategoryId?)default(CategoryId)),
            CancellationToken.None));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsCombinedRootAndParentFilters()
    {
        var repository = new CategoryListRepositoryFake();
        var useCase = new UseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new ListCategoriesQuery(ParentCategoryId: CategoryId.New(), RootsOnly: true),
            CancellationToken.None));

        Assert.Equal(0, repository.ListCalls);
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 101)]
    public async Task ExecuteAsync_RejectsInvalidPageBeforeRead(int offset, int limit)
    {
        var repository = new CategoryListRepositoryFake();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new UseCase(repository).ExecuteAsync(
            new ListCategoriesQuery(Offset: offset, Limit: limit), CancellationToken.None));
        Assert.Equal(0, repository.ListCalls);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 100)]
    [InlineData(2147483647, 50)]
    public async Task ExecuteAsync_ForwardsValidPageAndRootFilter(int offset, int limit)
    {
        var repository = new CategoryListRepositoryFake();
        await new UseCase(repository).ExecuteAsync(
            new ListCategoriesQuery(RootsOnly: true, Offset: offset, Limit: limit), CancellationToken.None);
        Assert.Equal(offset, repository.Offset);
        Assert.Equal(limit, repository.Limit);
        Assert.True(repository.RootsOnly);
    }

    private sealed class CategoryListRepositoryFake : ICategoryListRepository
    {
        public IReadOnlyList<CategoryListItem> Categories { get; set; } = [];
        public Exception? Exception { get; set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public int ListCalls { get; private set; }
        public CancellationToken Token { get; private set; }
        public string? SearchTerm { get; private set; }
        public CategoryId? ParentCategoryId { get; private set; }
        public bool RootsOnly { get; private set; }

        public Task<IReadOnlyList<CategoryListItem>> ListAsync(
            int offset,
            int limit,
            string? searchTerm,
            CategoryId? parentCategoryId,
            bool rootsOnly,
            CancellationToken cancellationToken)
        {
            Offset = offset;
            Limit = limit;
            ListCalls++;
            SearchTerm = searchTerm;
            ParentCategoryId = parentCategoryId;
            RootsOnly = rootsOnly;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<IReadOnlyList<CategoryListItem>>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Categories);
        }
    }
}
