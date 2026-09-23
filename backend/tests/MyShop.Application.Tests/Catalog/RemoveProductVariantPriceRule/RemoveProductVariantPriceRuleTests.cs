using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RemoveProductVariantPriceRule;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductVariantPriceRule.RemoveProductVariantPriceRule;

namespace MyShop.Application.Tests.Catalog.RemoveProductVariantPriceRule;

public sealed class RemoveProductVariantPriceRuleTests
{
    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    [Fact]
    public async Task ExecuteAsync_RemovesRuleAndSavesWithReadToken()
    {
        var scenario = new Scenario(withRule: true);

        var result = await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(scenario.Variant.PriceRules);
        Assert.Equal(Money.Create(50m, "EUR"), scenario.Variant.Price);
        Assert.Equal(1, scenario.Repository.SaveCalls);
        Assert.Same(scenario.Token, scenario.Repository.SavedToken);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutRule_IsIdempotentWithoutSave()
    {
        var scenario = new Scenario(withRule: false);

        var result = await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductMissing_ReturnsFailureWithoutSave()
    {
        var scenario = new Scenario(withRule: true) { ProductMissing = true };

        var result = await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.Equal(RemoveProductVariantPriceRuleFailure.ProductNotFound, result.Failure);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVariantMissing_ReturnsFailureWithoutSave()
    {
        var scenario = new Scenario(withRule: true);
        var command = scenario.Command with { ProductVariantId = ProductVariantId.New() };

        var result = await scenario.UseCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(RemoveProductVariantPriceRuleFailure.VariantNotFound, result.Failure);
        Assert.Equal(0, scenario.Repository.SaveCalls);
        Assert.Single(scenario.Variant.PriceRules);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesConcurrencyConflict()
    {
        var scenario = new Scenario(withRule: true) { Conflict = true };

        await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None));

        Assert.Equal(1, scenario.Repository.SaveCalls);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task ExecuteAsync_RejectsEmptyIdentifiers(bool emptyProductId, bool emptyVariantId, bool emptyRuleId)
    {
        var scenario = new Scenario(withRule: true);
        var command = scenario.Command with
        {
            ProductId = emptyProductId ? default : scenario.Command.ProductId,
            ProductVariantId = emptyVariantId ? default : scenario.Command.ProductVariantId,
            PriceRuleId = emptyRuleId ? Guid.Empty : scenario.Command.PriceRuleId
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
        var scenario = new Scenario(withRule: true);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Command, source.Token));
        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RuleOnAnotherVariantIsNotRemoved()
    {
        var scenario = new Scenario(true);
        var second = scenario.Product.AddVariant("Second");
        var result = await scenario.UseCase.ExecuteAsync(
            scenario.Command with { ProductVariantId = second.Id }, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Same(scenario.Rule, Assert.Single(scenario.Variant.PriceRules));
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RemovesOnlySelectedRuleAndForwardsCancellation()
    {
        var scenario = new Scenario(true);
        var retained = PriceRule.Create("Retained", PriceAdjustmentType.FixedDiscount, 2m, 0);
        scenario.Product.AddVariantPriceRule(scenario.Variant.Id, retained);
        using var source = new CancellationTokenSource();

        await scenario.UseCase.ExecuteAsync(scenario.Command, source.Token);

        Assert.Same(retained, Assert.Single(scenario.Variant.PriceRules));
        Assert.Equal(source.Token, scenario.Repository.ReadCancellation);
        Assert.Equal(source.Token, scenario.Repository.SaveCancellation);
    }

    private sealed class Scenario
    {
        public Scenario(bool withRule)
        {
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            Variant = Product.Variants.Single();
            Product.SetVariantPrice(Variant.Id, Money.Create(50m, "EUR"));
            Rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 5m, 1);
            if (withRule)
                Product.AddVariantPriceRule(Variant.Id, Rule);
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
            Repository = new RepositoryFake(this);
            UseCase = new UseCase(Repository);
            Command = new(Product.Id, Variant.Id, Rule.Id);
        }

        public PriceRule Rule { get; }
        public Product Product { get; }
        public ProductVariant Variant { get; }
        public ProductConcurrencyToken Token { get; }
        public RepositoryFake Repository { get; }
        public UseCase UseCase { get; }
        public RemoveProductVariantPriceRuleCommand Command { get; }
        public bool ProductMissing { get; init; }
        public bool Conflict { get; init; }
    }

    private sealed class RepositoryFake(Scenario scenario) : IProductRepository
    {
        public CancellationToken ReadCancellation { get; private set; }
        public CancellationToken SaveCancellation { get; private set; }
        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public ProductConcurrencyToken? SavedToken { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            ReadCancellation = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            GetCalls++;
            return Task.FromResult(scenario.ProductMissing ? null : new ProductSnapshot(scenario.Product, scenario.Token));
        }

        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken)
        {
            SaveCancellation = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Same(scenario.Product, product);
            SaveCalls++;
            SavedToken = expectedToken;
            return scenario.Conflict
                ? Task.FromException<ProductConcurrencyToken>(new ProductConcurrencyException(product.Id))
                : Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }
}
