using MyShop.Application.Catalog.Abstractions;
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

        var result = await useCase.ExecuteAsync(source.Token);

        Assert.Same(repository.Categories, result);
        Assert.Equal(source.Token, repository.Token);
        Assert.Equal(1, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepository()
    {
        var repository = new CategoryListRepositoryFake();
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(source.Token));

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
            useCase.ExecuteAsync(CancellationToken.None));

        Assert.Same(expected, exception);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    private sealed class CategoryListRepositoryFake : ICategoryListRepository
    {
        public IReadOnlyList<CategoryListItem> Categories { get; set; } = [];
        public Exception? Exception { get; set; }
        public int ListCalls { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken cancellationToken)
        {
            ListCalls++;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<IReadOnlyList<CategoryListItem>>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Categories);
        }
    }
}
