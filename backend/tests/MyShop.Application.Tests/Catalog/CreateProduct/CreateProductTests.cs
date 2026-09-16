using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.CreateProduct;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateProduct.CreateProduct;

namespace MyShop.Application.Tests.Catalog.CreateProduct;

public sealed class CreateProductTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesAndAddsProductAndReturnsSnapshot()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        var snapshot = Assert.IsType<ProductSnapshot>(result.Snapshot);
        Assert.Same(scenario.Products.AddedProduct, snapshot.Product);
        Assert.Same(scenario.Products.ReturnedToken, snapshot.ConcurrencyToken);
        Assert.Equal(scenario.Command.ProductTypeId, snapshot.Product.ProductTypeId);
        Assert.Equal(scenario.Command.Name, snapshot.Product.Name);
        Assert.Equal(scenario.Command.InitialVariantName, snapshot.Product.Variants.Single().Name);
        Assert.Equal(1, scenario.Products.AddCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductTypeIsMissing_ReturnsFailureWithoutCreatingProduct()
    {
        var scenario = new Scenario { ProductType = null };

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateProductFailure.ProductTypeNotFound, result.Failure);
        Assert.Null(result.Snapshot);
        Assert.Equal(0, scenario.Products.AddCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsProductTypeIdAndCancellationToken()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.Handler.ExecuteAsync(scenario.Command, source.Token);

        Assert.Equal(scenario.Command.ProductTypeId, scenario.RequestedProductTypeId);
        Assert.Equal(source.Token, scenario.ProductTypeToken);
        Assert.Equal(source.Token, scenario.Products.AddToken);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesDomainValidationWithoutAdding()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { Name = " " }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.AddCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullCommandAndDefaultProductTypeBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.Handler.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { ProductTypeId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.ProductTypeCalls);
        Assert.Equal(0, scenario.Products.AddCalls);
    }

    [Fact]
    public void Constructor_RejectsNullRepositories()
    {
        var scenario = new Scenario();

        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, scenario));
        Assert.Throws<ArgumentNullException>(() => new UseCase(scenario.Products, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepositories()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, source.Token));

        Assert.Equal(0, scenario.ProductTypeCalls);
        Assert.Equal(0, scenario.Products.AddCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesRepositoryExceptions()
    {
        var scenario = new Scenario();
        var expected = new InvalidOperationException("failure");
        scenario.Products.AddException = expected;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None));

        Assert.Same(expected, exception);
    }

    private sealed class Scenario : IProductTypeRepository
    {
        public Scenario()
        {
            ProductType = Domain.Catalog.ProductType.Create("Type");
            Products = new ProductRepositoryFake();
            Handler = new UseCase(Products, this);
            Command = new CreateProductCommand(ProductType.Id, "Product", "Standard");
        }

        public ProductType? ProductType { get; set; }
        public ProductRepositoryFake Products { get; }
        public UseCase Handler { get; }
        public CreateProductCommand Command { get; }
        public int ProductTypeCalls { get; private set; }
        public ProductTypeId RequestedProductTypeId { get; private set; }
        public CancellationToken ProductTypeToken { get; private set; }

        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            ProductTypeCalls++;
            RequestedProductTypeId = id;
            ProductTypeToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductType);
        }
    }

    private sealed class ProductRepositoryFake : IProductRepository
    {
        public Exception? AddException { get; set; }
        public int AddCalls { get; private set; }
        public Product? AddedProduct { get; private set; }
        public CancellationToken AddToken { get; private set; }
        public ProductConcurrencyToken? ReturnedToken { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken)
        {
            AddCalls++;
            AddedProduct = product;
            AddToken = cancellationToken;
            if (AddException is not null)
                return Task.FromException<ProductConcurrencyToken>(AddException);
            cancellationToken.ThrowIfCancellationRequested();
            ReturnedToken = ProductConcurrencyToken.Create(product.Id, Guid.NewGuid());
            return Task.FromResult(ReturnedToken);
        }

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
