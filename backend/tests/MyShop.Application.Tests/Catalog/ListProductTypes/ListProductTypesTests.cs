using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ListProductTypes;
using UseCase = MyShop.Application.Catalog.ListProductTypes.ListProductTypes;

namespace MyShop.Application.Tests.Catalog.ListProductTypes;

public sealed class ListProductTypesTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsRepositoryResultsAndForwardsToken()
    {
        var repository = new ProductTypeListRepositoryFake
        {
            ProductTypes = [new(Guid.NewGuid(), "Clothing", 3), new(Guid.NewGuid(), "Shoes", 2)]
        };
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(new ListProductTypesQuery("  cloth  "), source.Token);

        Assert.Same(repository.ProductTypes, result);
        Assert.Equal(source.Token, repository.Token);
        Assert.Equal(1, repository.ListCalls);
        Assert.Equal("cloth", repository.SearchTerm);
        Assert.Equal(0, repository.Offset);
        Assert.Equal(50, repository.Limit);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepository()
    {
        var repository = new ProductTypeListRepositoryFake();
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(new ListProductTypesQuery(), source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesRepositoryException()
    {
        var repository = new ProductTypeListRepositoryFake();
        var expected = new InvalidOperationException("failure");
        repository.Exception = expected;
        var useCase = new UseCase(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new ListProductTypesQuery(), CancellationToken.None));

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
        var repository = new ProductTypeListRepositoryFake();
        var useCase = new UseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new ListProductTypesQuery(search), CancellationToken.None));

        Assert.Equal(0, repository.ListCalls);
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 101)]
    public async Task ExecuteAsync_RejectsInvalidPageBeforeRead(int offset, int limit)
    {
        var repository = new ProductTypeListRepositoryFake();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new UseCase(repository).ExecuteAsync(
            new ListProductTypesQuery(Offset: offset, Limit: limit), CancellationToken.None));
        Assert.Equal(0, repository.ListCalls);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 100)]
    [InlineData(2147483647, 50)]
    public async Task ExecuteAsync_ForwardsValidPageAndTrimmedSearch(int offset, int limit)
    {
        var repository = new ProductTypeListRepositoryFake();
        await new UseCase(repository).ExecuteAsync(
            new ListProductTypesQuery("  type  ", offset, limit), CancellationToken.None);
        Assert.Equal(offset, repository.Offset);
        Assert.Equal(limit, repository.Limit);
        Assert.Equal("type", repository.SearchTerm);
    }

    private sealed class ProductTypeListRepositoryFake : IProductTypeListRepository
    {
        public IReadOnlyList<ProductTypeListItem> ProductTypes { get; set; } = [];
        public Exception? Exception { get; set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public int ListCalls { get; private set; }
        public CancellationToken Token { get; private set; }
        public string? SearchTerm { get; private set; }

        public Task<IReadOnlyList<ProductTypeListItem>> ListAsync(
            int offset,
            int limit,
            string? searchTerm,
            CancellationToken cancellationToken)
        {
            Offset = offset;
            Limit = limit;
            ListCalls++;
            SearchTerm = searchTerm;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<IReadOnlyList<ProductTypeListItem>>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductTypes);
        }
    }
}
