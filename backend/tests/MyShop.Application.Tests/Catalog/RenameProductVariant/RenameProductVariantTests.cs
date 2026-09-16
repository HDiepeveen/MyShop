using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RenameProductVariant;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductVariant.RenameProductVariant;

namespace MyShop.Application.Tests.Catalog.RenameProductVariant;

public sealed class RenameProductVariantTests
{
    [Fact]
    public async Task ExecuteAsync_RenamesVariantAndSavesWithReadToken()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(scenario.Command.Name, scenario.Variant.Name);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Product, scenario.Products.SavedProduct);
        Assert.Same(scenario.Products.ReadToken, scenario.Products.SavedExpectedToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameIsUnchanged_SucceedsWithoutSaving()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command with { Name = scenario.Variant.Name },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIsMissing_ReturnsFailureWithoutSaving()
    {
        var scenario = new Scenario { ProductIsMissing = true };

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RenameProductVariantFailure.ProductNotFound, result.Failure);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVariantIsMissing_ReturnsFailureWithoutMutation()
    {
        var scenario = new Scenario();
        var originalName = scenario.Variant.Name;

        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command with { ProductVariantId = ProductVariantId.New() },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RenameProductVariantFailure.VariantNotFound, result.Failure);
        Assert.Equal(originalName, scenario.Variant.Name);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesDomainValidationWithoutMutationOrSave()
    {
        var scenario = new Scenario();
        var originalName = scenario.Variant.Name;

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { Name = " " }, CancellationToken.None));

        Assert.Equal(originalName, scenario.Variant.Name);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsIdsAndCancellationToken()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.Handler.ExecuteAsync(scenario.Command, source.Token);

        Assert.Equal(scenario.Command.ProductId, scenario.Products.RequestedId);
        Assert.Equal(source.Token, scenario.Products.GetToken);
        Assert.Equal(source.Token, scenario.Products.SaveToken);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInvalidArgumentsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.Handler.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { ProductId = default }, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { ProductVariantId = default }, CancellationToken.None));

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
            scenario.Handler.ExecuteAsync(scenario.Command, source.Token));

        Assert.Equal(0, scenario.Products.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesConcurrencyConflictAfterMutation()
    {
        var scenario = new Scenario();
        var expected = new ProductConcurrencyException(scenario.Product.Id);
        scenario.Products.SaveException = expected;

        var exception = await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None));

        Assert.Same(expected, exception);
        Assert.Equal(scenario.Command.Name, scenario.Variant.Name);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Domain.Catalog.Product.Create("Product", ProductTypeId.New(), "Original");
            Variant = Product.Variants.Single();
            Products = new ProductRepositoryFake(this);
            Handler = new UseCase(Products);
            Command = new RenameProductVariantCommand(Product.Id, Variant.Id, "Renamed");
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public bool ProductIsMissing { get; set; }
        public ProductRepositoryFake Products { get; }
        public UseCase Handler { get; }
        public RenameProductVariantCommand Command { get; }
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        public Exception? SaveException { get; set; }
        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public ProductId RequestedId { get; private set; }
        public CancellationToken GetToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public Product? SavedProduct { get; private set; }
        public ProductConcurrencyToken? ReadToken { get; private set; }
        public ProductConcurrencyToken? SavedExpectedToken { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            GetToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (scenario.ProductIsMissing)
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
            SavedProduct = product;
            SavedExpectedToken = expectedToken;
            SaveToken = cancellationToken;
            if (SaveException is not null)
                return Task.FromException<ProductConcurrencyToken>(SaveException);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }
}
