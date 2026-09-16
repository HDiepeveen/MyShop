using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.AddProductVariant;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AddProductVariant.AddProductVariant;

namespace MyShop.Application.Tests.Catalog.AddProductVariant;

public sealed class AddProductVariantTests
{
    [Fact]
    public async Task ExecuteAsync_AddsVariantAndReturnsSnapshotWithReplacementToken()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        var snapshot = Assert.IsType<ProductSnapshot>(result.Snapshot);
        var product = Assert.IsType<Product>(scenario.Product);
        Assert.Same(product, snapshot.Product);
        Assert.Same(scenario.Products.ReturnedToken, snapshot.ConcurrencyToken);
        Assert.Equal(["Standard", scenario.Command.Name],
            product.Variants.Select(variant => variant.Name));
        Assert.NotEqual(default, product.Variants.Last().Id);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Products.ReadToken, scenario.Products.SavedExpectedToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIsMissing_ReturnsFailureWithoutSaving()
    {
        var scenario = new Scenario { Product = null };

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AddProductVariantFailure.ProductNotFound, result.Failure);
        Assert.Null(result.Snapshot);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesDomainValidationWithoutSaving()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { Name = " " }, CancellationToken.None));

        Assert.Single(Assert.IsType<Product>(scenario.Product).Variants);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsIdAndCancellationToken()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.Handler.ExecuteAsync(scenario.Command, source.Token);

        Assert.Equal(scenario.Command.ProductId, scenario.Products.RequestedId);
        Assert.Equal(source.Token, scenario.Products.GetToken);
        Assert.Equal(source.Token, scenario.Products.SaveToken);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullCommandAndDefaultProductIdBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.Handler.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { ProductId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
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
            scenario.Handler.ExecuteAsync(scenario.Command, source.Token));

        Assert.Equal(0, scenario.Products.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesConcurrencyConflictAfterMutation()
    {
        var scenario = new Scenario();
        var product = Assert.IsType<Product>(scenario.Product);
        var expected = new ProductConcurrencyException(product.Id);
        scenario.Products.SaveException = expected;

        var exception = await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None));

        Assert.Same(expected, exception);
        Assert.Equal(2, product.Variants.Count);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Domain.Catalog.Product.Create("Product", ProductTypeId.New(), "Standard");
            Products = new ProductRepositoryFake(this);
            Handler = new UseCase(Products);
            Command = new AddProductVariantCommand(Product.Id, "Second");
        }

        public Product? Product { get; set; }
        public ProductRepositoryFake Products { get; }
        public UseCase Handler { get; }
        public AddProductVariantCommand Command { get; }
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        public Exception? SaveException { get; set; }
        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public ProductId RequestedId { get; private set; }
        public CancellationToken GetToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public ProductConcurrencyToken? ReadToken { get; private set; }
        public ProductConcurrencyToken? SavedExpectedToken { get; private set; }
        public ProductConcurrencyToken? ReturnedToken { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            GetToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (scenario.Product is null)
                return Task.FromResult<ProductSnapshot?>(null);
            ReadToken ??= ProductConcurrencyToken.Create(scenario.Product.Id, Guid.NewGuid());
            return Task.FromResult<ProductSnapshot?>(new ProductSnapshot(scenario.Product, ReadToken));
        }

        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken)
        {
            SaveCalls++;
            SavedExpectedToken = expectedToken;
            SaveToken = cancellationToken;
            if (SaveException is not null)
                return Task.FromException<ProductConcurrencyToken>(SaveException);
            cancellationToken.ThrowIfCancellationRequested();
            ReturnedToken = ProductConcurrencyToken.Create(product.Id, Guid.NewGuid());
            return Task.FromResult(ReturnedToken);
        }
    }
}
