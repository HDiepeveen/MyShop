using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.AddProductVariantPriceRule;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AddProductVariantPriceRule.AddProductVariantPriceRule;

namespace MyShop.Application.Tests.Catalog.AddProductVariantPriceRule;

public sealed class AddProductVariantPriceRuleTests
{
    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    [Fact]
    public async Task ExecuteAsync_AddsRuleAndReturnsItsIdentityWithReadToken()
    {
        var scenario = new Scenario(withSku: true);

        var result = await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var rule = Assert.Single(scenario.Variant.PriceRules);
        Assert.Equal(rule.Id, result.PriceRuleId);
        Assert.Equal("Sale", rule.Name);
        Assert.Equal(10m, rule.Value);
        Assert.Equal(PriceAdjustmentType.PercentageDiscount, rule.AdjustmentType);
        Assert.Equal(5, rule.Priority);
        Assert.Equal(scenario.Command.StartsAt, rule.StartsAt);
        Assert.Equal(scenario.Command.EndsAt, rule.EndsAt);
        Assert.Equal("SKU-1", scenario.Variant.Sku!.Value);
        Assert.Equal(1, scenario.Repository.SaveCalls);
        Assert.Same(scenario.Token, scenario.Repository.SavedToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductMissing_ReturnsFailureWithoutSave()
    {
        var scenario = new Scenario(withSku: true) { ProductMissing = true };

        var result = await scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.Equal(AddProductVariantPriceRuleFailure.ProductNotFound, result.Failure);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVariantMissing_ReturnsFailureWithoutSave()
    {
        var scenario = new Scenario(withSku: true);
        var command = scenario.Command with { ProductVariantId = ProductVariantId.New() };

        var result = await scenario.UseCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(AddProductVariantPriceRuleFailure.VariantNotFound, result.Failure);
        Assert.Equal(0, scenario.Repository.SaveCalls);
        Assert.NotNull(scenario.Variant.Sku);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesConcurrencyConflict()
    {
        var scenario = new Scenario(withSku: true) { Conflict = true };

        await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Command, CancellationToken.None));

        Assert.Equal(1, scenario.Repository.SaveCalls);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExecuteAsync_RejectsEmptyIdentifiers(bool emptyProductId, bool emptyVariantId)
    {
        var scenario = new Scenario(withSku: true);
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
        var scenario = new Scenario(withSku: true);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Command, source.Token));
        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("zero")]
    [InlineData("rounds to zero")]
    [InlineData("percentage")]
    [InlineData("period")]
    public async Task ExecuteAsync_InvalidRuleDoesNotMutateOrSave(string invalidField)
    {
        var scenario = new Scenario(true);
        var command = invalidField switch
        {
            "name" => scenario.Command with { Name = " " },
            "type" => scenario.Command with { AdjustmentType = (PriceAdjustmentType)99 },
            "zero" => scenario.Command with { Value = 0m },
            "rounds to zero" => scenario.Command with { Value = 0.004m },
            "percentage" => scenario.Command with { Value = 101m },
            _ => scenario.Command with { EndsAt = scenario.Command.StartsAt!.Value.AddDays(-1) }
        };
        await Assert.ThrowsAnyAsync<ArgumentException>(() => scenario.UseCase.ExecuteAsync(command, CancellationToken.None));
        Assert.Empty(scenario.Variant.PriceRules);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_AddsRuleOnlyToRequestedVariantAndForwardsCancellation()
    {
        var scenario = new Scenario(true);
        var second = scenario.Product.AddVariant("Second");
        using var source = new CancellationTokenSource();
        var result = await scenario.UseCase.ExecuteAsync(
            scenario.Command with { ProductVariantId = second.Id }, source.Token);
        Assert.True(result.IsSuccess);
        Assert.Empty(scenario.Variant.PriceRules);
        Assert.Equal(result.PriceRuleId, Assert.Single(second.PriceRules).Id);
        Assert.Equal(source.Token, scenario.Repository.ReadCancellation);
        Assert.Equal(source.Token, scenario.Repository.SaveCancellation);
    }

    private sealed class Scenario
    {
        public Scenario(bool withSku)
        {
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            Variant = Product.Variants.Single();
            if (withSku)
                Product.SetVariantSku(Variant.Id, Sku.Create("SKU-1"));
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
            Repository = new RepositoryFake(this);
            UseCase = new UseCase(Repository);
            Command = new(Product.Id, Variant.Id, " Sale ", PriceAdjustmentType.PercentageDiscount,
                10m, 5, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public ProductConcurrencyToken Token { get; }
        public RepositoryFake Repository { get; }
        public UseCase UseCase { get; }
        public AddProductVariantPriceRuleCommand Command { get; }
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
