using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.SetProductVariantSku;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetProductVariantSku.SetProductVariantSku;

namespace MyShop.Application.Tests.Catalog.SetProductVariantSku;

public sealed class SetProductVariantSkuTests
{
    [Fact]
    public async Task ExecuteAsync_AssignsFirstSkuAndSavesOnce()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("abc-123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ABC-123", scenario.Variant.Sku!.Value);
        Assert.Equal(1, scenario.Lookup.Calls);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Product, scenario.Products.SavedProduct);
    }

    [Fact]
    public async Task ExecuteAsync_ChangesExistingSku()
    {
        var scenario = new Scenario("OLD");

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("NEW"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("NEW", scenario.Variant.Sku!.Value);
        Assert.Equal(1, scenario.Lookup.Calls);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSameSku_ReturnsSuccessWithoutLookupOrSave()
    {
        var scenario = new Scenario("ABC-123");

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("ABC-123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, scenario.Lookup.Calls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSameSkuHasDifferentCasing_IsIdempotent()
    {
        var scenario = new Scenario("abc-123");

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("ABC-123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ABC-123", scenario.Variant.Sku!.Value);
        Assert.Equal(0, scenario.Lookup.Calls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIsMissing_ReturnsFailureWithoutLookupOrSave()
    {
        var scenario = new Scenario();
        scenario.Products.Product = null;

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("NEW"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SetProductVariantSkuFailure.ProductNotFound, result.Failure);
        Assert.Equal(0, scenario.Lookup.Calls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVariantIsMissing_ReturnsFailureWithoutLookupOrSave()
    {
        var scenario = new Scenario();
        var foreign = Product.Create("Foreign", ProductTypeId.New(), "Standard");
        var command = scenario.Command("NEW") with { ProductVariantId = Assert.Single(foreign.Variants).Id };

        var result = await scenario.Handler.ExecuteAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SetProductVariantSkuFailure.VariantNotFound, result.Failure);
        Assert.Equal(0, scenario.Lookup.Calls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLocalDuplicate_ReturnsFailureWithoutLookupOrSave()
    {
        var scenario = new Scenario("EXISTING");
        var other = scenario.Product.AddVariant("Other");

        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command("EXISTING") with { ProductVariantId = other.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SetProductVariantSkuFailure.SkuAlreadyInUse, result.Failure);
        Assert.Equal(0, scenario.Lookup.Calls);
        Assert.Equal(0, scenario.Products.SaveCalls);
        Assert.Equal("EXISTING", scenario.Variant.Sku!.Value);
        Assert.Null(other.Sku);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLocalDuplicateHasDifferentCasing_ReturnsFailure()
    {
        var scenario = new Scenario("existing");
        var other = scenario.Product.AddVariant("Other");

        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command("EXISTING") with { ProductVariantId = other.Id }, CancellationToken.None);

        Assert.Equal(SetProductVariantSkuFailure.SkuAlreadyInUse, result.Failure);
        Assert.Equal(0, scenario.Lookup.Calls);
        Assert.Null(other.Sku);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGlobalOwnerIsAnotherProduct_ReturnsFailureWithoutSaving()
    {
        var scenario = new Scenario();
        var owner = Product.Create("Owner", ProductTypeId.New(), "Standard");
        scenario.Lookup.Owner = new ProductSkuOwner(owner.Id, Assert.Single(owner.Variants).Id);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("DUPLICATE"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SetProductVariantSkuFailure.SkuAlreadyInUse, result.Failure);
        Assert.Equal(1, scenario.Lookup.Calls);
        Assert.Equal(0, scenario.Products.SaveCalls);
        Assert.Null(scenario.Variant.Sku);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGlobalOwnerIsAnotherVariant_ReturnsFailureWithoutSaving()
    {
        var scenario = new Scenario();
        var owner = Product.Create("Owner", ProductTypeId.New(), "Standard");
        scenario.Lookup.Owner = new ProductSkuOwner(owner.Id, Assert.Single(owner.Variants).Id);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("DUPLICATE"), CancellationToken.None);

        Assert.Equal(SetProductVariantSkuFailure.SkuAlreadyInUse, result.Failure);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLookupReturnsTargetOwner_AcceptsAndSaves()
    {
        var scenario = new Scenario();
        scenario.Lookup.Owner = new ProductSkuOwner(scenario.Product.Id, scenario.Variant.Id);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("NEW"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, scenario.Lookup.Calls);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Equal("NEW", scenario.Variant.Sku!.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSkuIsUnused_SucceedsAndSavesOnce()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("NEW"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(scenario.Lookup.Owner);
        Assert.Equal(1, scenario.Lookup.Calls);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCanonicalSkuAndIdsAndToken()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.Handler.ExecuteAsync(scenario.Command("abc-123"), source.Token);

        Assert.Equal(scenario.Product.Id, scenario.Products.RequestedId);
        Assert.Equal("ABC-123", scenario.Lookup.RequestedSku!.Value);
        Assert.Equal(source.Token, scenario.Products.GetToken);
        Assert.Equal(source.Token, scenario.Lookup.Token);
        Assert.Equal(source.Token, scenario.Products.SaveToken);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSku_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command(null!), CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Lookup.Calls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ABC DEF")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task ExecuteAsync_InvalidSkuValuesFailBeforeRepositoryAccess(string? value)
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command(value!), CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Lookup.Calls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultProductId_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command("NEW") with { ProductId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Lookup.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultVariantId_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command("NEW") with { ProductVariantId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Lookup.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullCommand_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.Handler.ExecuteAsync(null!, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Lookup.Calls);
    }

    [Fact]
    public void Constructor_WithNullDependencies_Throws()
    {
        var scenario = new Scenario();

        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, scenario.Lookup));
        Assert.Throws<ArgumentNullException>(() => new UseCase(scenario.Products, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledBeforeExecution_PropagatesWithoutRepositoryAccess()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command("NEW"), source.Token));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Lookup.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesProductRepositoryCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        var expected = new OperationCanceledException(source.Token);
        scenario.Products.GetException = expected;

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command("NEW"), source.Token));

        Assert.Same(expected, exception);
        Assert.Equal(0, scenario.Lookup.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesLookupCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        var expected = new OperationCanceledException(source.Token);
        scenario.Lookup.Exception = expected;

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command("NEW"), source.Token));

        Assert.Same(expected, exception);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesSaveCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        var expected = new OperationCanceledException(source.Token);
        scenario.Products.SaveException = expected;

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command("NEW"), source.Token));

        Assert.Same(expected, exception);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Equal("NEW", scenario.Variant.Sku!.Value);
    }

    [Fact]
    public async Task ExecuteAsync_ChangingOneVariantPreservesOtherVariants()
    {
        var scenario = new Scenario("FIRST");
        var other = scenario.Product.AddVariant("Other");
        scenario.Product.SetVariantSku(other.Id, Sku.Create("SECOND"));

        var result = await scenario.Handler.ExecuteAsync(scenario.Command("UPDATED"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("UPDATED", scenario.Variant.Sku!.Value);
        Assert.Equal("SECOND", other.Sku!.Value);
    }

    private sealed class Scenario
    {
        public Scenario(string? sku = null)
        {
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            Variant = Assert.Single(Product.Variants);
            if (sku is not null)
                Product.SetVariantSku(Variant.Id, Sku.Create(sku));

            Products = new ProductRepositoryFake(Product);
            Lookup = new ProductSkuLookupFake();
            Handler = new UseCase(Products, Lookup);
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public ProductRepositoryFake Products { get; }
        public ProductSkuLookupFake Lookup { get; }
        public UseCase Handler { get; }

        public SetProductVariantSkuCommand Command(string sku) =>
            new(Product.Id, Variant.Id, sku);
    }

    private sealed class ProductRepositoryFake(Product product) : IProductRepository
    {
        public Product? Product { get; set; } = product;
        public Exception? GetException { get; set; }
        public Exception? SaveException { get; set; }
        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public ProductId RequestedId { get; private set; }
        public CancellationToken GetToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public Product? SavedProduct { get; private set; }

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            GetToken = cancellationToken;
            if (GetException is not null)
                return Task.FromException<Product?>(GetException);

            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Product);
        }

        public Task SaveAsync(Product product, CancellationToken cancellationToken)
        {
            SaveCalls++;
            SavedProduct = product;
            SaveToken = cancellationToken;
            if (SaveException is not null)
                return Task.FromException(SaveException);

            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class ProductSkuLookupFake : IProductSkuLookup
    {
        public ProductSkuOwner? Owner { get; set; }
        public Exception? Exception { get; set; }
        public int Calls { get; private set; }
        public Sku? RequestedSku { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<ProductSkuOwner?> FindOwnerAsync(Sku sku, CancellationToken cancellationToken)
        {
            Calls++;
            RequestedSku = sku;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<ProductSkuOwner?>(Exception);

            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Owner);
        }
    }
}