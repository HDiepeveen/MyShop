using MyShop.Application.Catalog.Abstractions;
using UseCase = MyShop.Application.Catalog.ListProductTypes.ListProductTypes;

namespace MyShop.Application.Tests.Catalog.ListProductTypes;

public sealed class ListProductTypesTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsRepositoryResultsAndForwardsToken()
    {
        var repository = new ProductTypeListRepositoryFake
        {
            ProductTypes = [new(Guid.NewGuid(), "Clothing"), new(Guid.NewGuid(), "Shoes")]
        };
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(source.Token);

        Assert.Same(repository.ProductTypes, result);
        Assert.Equal(source.Token, repository.Token);
        Assert.Equal(1, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepository()
    {
        var repository = new ProductTypeListRepositoryFake();
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
        var repository = new ProductTypeListRepositoryFake();
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

    private sealed class ProductTypeListRepositoryFake : IProductTypeListRepository
    {
        public IReadOnlyList<ProductTypeListItem> ProductTypes { get; set; } = [];
        public Exception? Exception { get; set; }
        public int ListCalls { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<IReadOnlyList<ProductTypeListItem>> ListAsync(CancellationToken cancellationToken)
        {
            ListCalls++;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<IReadOnlyList<ProductTypeListItem>>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductTypes);
        }
    }
}
