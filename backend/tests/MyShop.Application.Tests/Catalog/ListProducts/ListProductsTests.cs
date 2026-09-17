using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ListProducts;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListProducts.ListProducts;

namespace MyShop.Application.Tests.Catalog.ListProducts;

public sealed class ListProductsTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsPageAndForwardsQueryAndToken()
    {
        var expected = new ProductListPage(
            [new ProductListItem(Guid.NewGuid(), Guid.NewGuid(), "Shirt", 2)],
            12);
        var repository = new ProductListRepositoryFake { Page = expected };
        var useCase = new UseCase(repository);
        var productTypeId = ProductTypeId.New();
        var query = new ListProductsQuery(5, 10, productTypeId);
        using var source = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(query, source.Token);

        Assert.Same(expected, result);
        Assert.Equal(5, repository.Offset);
        Assert.Equal(10, repository.Limit);
        Assert.Equal(productTypeId, repository.ProductTypeId);
        Assert.Equal(source.Token, repository.Token);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public async Task ExecuteAsync_RejectsInvalidPagingBeforeRepositoryAccess(int offset, int limit)
    {
        var repository = new ProductListRepositoryFake();
        var useCase = new UseCase(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            useCase.ExecuteAsync(new ListProductsQuery(offset, limit), CancellationToken.None));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullQueryBeforeRepositoryAccess()
    {
        var repository = new ProductListRepositoryFake();
        var useCase = new UseCase(repository);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(null!, CancellationToken.None));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsEmptyProductTypeIdBeforeRepositoryAccess()
    {
        var repository = new ProductListRepositoryFake();
        var useCase = new UseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(
                new ListProductsQuery(0, 50, (ProductTypeId?)default(ProductTypeId)),
                CancellationToken.None));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepository()
    {
        var repository = new ProductListRepositoryFake();
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(new ListProductsQuery(0, 50), source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesRepositoryException()
    {
        var repository = new ProductListRepositoryFake();
        var expected = new InvalidOperationException("failure");
        repository.Exception = expected;
        var useCase = new UseCase(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new ListProductsQuery(0, 50), CancellationToken.None));

        Assert.Same(expected, exception);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    private sealed class ProductListRepositoryFake : IProductListRepository
    {
        public ProductListPage Page { get; set; } = new([], 0);
        public Exception? Exception { get; set; }
        public int ListCalls { get; private set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public ProductTypeId? ProductTypeId { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<ProductListPage> ListAsync(
            int offset,
            int limit,
            ProductTypeId? productTypeId,
            CancellationToken cancellationToken)
        {
            ListCalls++;
            Offset = offset;
            Limit = limit;
            ProductTypeId = productTypeId;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<ProductListPage>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Page);
        }
    }
}
