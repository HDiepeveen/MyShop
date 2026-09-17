using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ListCategories;
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
                new CategoryListItem(Guid.NewGuid(), "Clothing", null),
                new CategoryListItem(Guid.NewGuid(), "Shirts", Guid.NewGuid())
            ]
        };
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(new ListCategoriesQuery("  shirt  "), source.Token);

        Assert.Same(repository.Categories, result);
        Assert.Equal(source.Token, repository.Token);
        Assert.Equal(1, repository.ListCalls);
        Assert.Equal("shirt", repository.SearchTerm);
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

    private sealed class CategoryListRepositoryFake : ICategoryListRepository
    {
        public IReadOnlyList<CategoryListItem> Categories { get; set; } = [];
        public Exception? Exception { get; set; }
        public int ListCalls { get; private set; }
        public CancellationToken Token { get; private set; }
        public string? SearchTerm { get; private set; }

        public Task<IReadOnlyList<CategoryListItem>> ListAsync(
            string? searchTerm,
            CancellationToken cancellationToken)
        {
            ListCalls++;
            SearchTerm = searchTerm;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<IReadOnlyList<CategoryListItem>>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Categories);
        }
    }
}
