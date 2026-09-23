using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ClearProductVariantPrice;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ClearProductVariantPrice.ClearProductVariantPrice;

namespace MyShop.Application.Tests.Catalog.ClearProductVariantPrice;

public sealed class ClearProductVariantPriceTests
{
    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    [Fact]
    public async Task ExecuteAsync_ClearsPriceAndSavesWithReadToken()
    {
        var scenario = new Scenario(withPrice: true);

        var result = await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(scenario.Variant.Price);
        Assert.Equal(1, scenario.Repository.SaveCalls);
        Assert.Same(scenario.Token, scenario.Repository.SavedToken);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutPrice_IsIdempotentWithoutSave()
    {
        var scenario = new Scenario(withPrice: false);

        var result = await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductMissing_ReturnsFailureWithoutSave()
    {
        var scenario = new Scenario(withPrice: true) { ProductMissing = true };

        var result = await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.Equal(ClearProductVariantPriceFailure.ProductNotFound, result.Failure);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVariantMissing_ReturnsFailureWithoutSave()
    {
        var scenario = new Scenario(withPrice: true);
        var command = scenario.Command with { ProductVariantId = ProductVariantId.New() };

        var result = await scenario.UseCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(ClearProductVariantPriceFailure.VariantNotFound, result.Failure);
        Assert.Equal(0, scenario.Repository.SaveCalls);
        Assert.NotNull(scenario.Variant.Price);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesConcurrencyConflict()
    {
        var scenario = new Scenario(withPrice: true) { Conflict = true };

        await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None));

        Assert.Equal(1, scenario.Repository.SaveCalls);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExecuteAsync_RejectsEmptyIdentifiers(bool emptyProductId, bool emptyVariantId)
    {
        var scenario = new Scenario(withPrice: true);
        var command = scenario.Command with
        {
            ProductId = emptyProductId ? default : scenario.Command.ProductId,
            ProductVariantId = emptyVariantId ? default : scenario.Command.ProductVariantId
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            scenario.UseCase.ExecuteAsync(command, CancellationToken.None));
        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullCommand() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new Scenario(true).UseCase.ExecuteAsync(null!, CancellationToken.None));

    [Fact]
    public async Task ExecuteAsync_HonorsPreCanceledToken()
    {
        var scenario = new Scenario(withPrice: true);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Command, source.Token));
        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ClearingBasePricePreservesAssignedRules()
    {
        var scenario = new Scenario(true);
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 1m, 0);
        scenario.Product.AddVariantPriceRule(scenario.Variant.Id, rule);

        await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.Null(scenario.Variant.Price);
        Assert.Same(rule, Assert.Single(scenario.Variant.PriceRules));
    }

    private sealed class Scenario
    {
        public Scenario(bool withPrice)
        {
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            Variant = Product.Variants.Single();
            if (withPrice)
                Product.SetVariantPrice(Variant.Id, Money.Create(9.99m, "EUR"));
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
            Repository = new RepositoryFake(this);
            UseCase = new UseCase(Repository);
            Command = new(Product.Id, Variant.Id);
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public ProductConcurrencyToken Token { get; }
        public RepositoryFake Repository { get; }
        public UseCase UseCase { get; }
        public ClearProductVariantPriceCommand Command { get; }
        public bool ProductMissing { get; init; }
        public bool Conflict { get; init; }
    }

    private sealed class RepositoryFake(Scenario scenario) : IProductRepository
    {
        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public ProductConcurrencyToken? SavedToken { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(scenario.ProductMissing ? null : new ProductSnapshot(scenario.Product, scenario.Token));
        }

        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken)
        {
            SaveCalls++;
            SavedToken = expectedToken;
            return scenario.Conflict
                ? Task.FromException<ProductConcurrencyToken>(new ProductConcurrencyException(product.Id))
                : Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }
}
