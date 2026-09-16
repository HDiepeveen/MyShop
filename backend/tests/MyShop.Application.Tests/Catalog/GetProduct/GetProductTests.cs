using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProduct;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProduct.GetProduct;

namespace MyShop.Application.Tests.Catalog.GetProduct;

public sealed class GetProductTests
{
    [Fact]
    public async Task ExecuteAsync_WhenProductExists_ReturnsRepositorySnapshot()
    {
        var scenario = new Scenario();

        var result = await scenario.UseCase.ExecuteAsync(scenario.Query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Same(scenario.Snapshot, result.Snapshot);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductDoesNotExist_ReturnsFailure()
    {
        var scenario = new Scenario { Snapshot = null };

        var result = await scenario.UseCase.ExecuteAsync(scenario.Query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetProductFailure.ProductNotFound, result.Failure);
        Assert.Null(result.Snapshot);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsIdAndCancellationToken()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.UseCase.ExecuteAsync(scenario.Query, source.Token);

        Assert.Equal(scenario.Query.ProductId, scenario.Products.RequestedId);
        Assert.Equal(source.Token, scenario.Products.GetToken);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInvalidArgumentsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.UseCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            scenario.UseCase.ExecuteAsync(new GetProductQuery(default), CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepository()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Query, source.Token));

        Assert.Equal(0, scenario.Products.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesRepositoryException()
    {
        var scenario = new Scenario();
        var expected = new InvalidOperationException("failure");
        scenario.Products.Exception = expected;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Query, CancellationToken.None));

        Assert.Same(expected, exception);
    }

    private sealed class Scenario
    {
        private ProductSnapshot? _snapshot;

        public Scenario()
        {
            var product = Product.Create("Product", ProductTypeId.New(), "Standard");
            _snapshot = new ProductSnapshot(
                product,
                ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
            Products = new ProductRepositoryFake(this);
            UseCase = new UseCase(Products);
            Query = new GetProductQuery(product.Id);
        }

        public ProductSnapshot? Snapshot
        {
            get => _snapshot;
            set => _snapshot = value;
        }

        public ProductRepositoryFake Products { get; }
        public UseCase UseCase { get; }
        public GetProductQuery Query { get; }
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        public Exception? Exception { get; set; }
        public int GetCalls { get; private set; }
        public ProductId RequestedId { get; private set; }
        public CancellationToken GetToken { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(
            ProductId id,
            CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            GetToken = cancellationToken;
            if (Exception is not null)
                return Task.FromException<ProductSnapshot?>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.Snapshot);
        }

        public Task<ProductConcurrencyToken> AddAsync(
            Product product,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
