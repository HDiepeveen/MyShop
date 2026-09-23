using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductVariantPrice;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductVariantPrice.GetProductVariantPrice;

namespace MyShop.Application.Tests.Catalog.GetProductVariantPrice;

public sealed class GetProductVariantPriceTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));

    [Theory]
    [InlineData(-1, 90)]
    [InlineData(0, 75)]
    [InlineData(1, 75)]
    [InlineData(2, 90)]
    public async Task ExecuteAsync_UsesHighestPriorityActiveRuleAtExplicitInstant(int hours, int amount)
    {
        var repository = new RepositoryFake();
        var variant = repository.Product.Variants.Single();
        repository.Product.SetVariantPrice(variant.Id, Money.Create(100m, "EUR"));
        repository.Product.AddVariantPriceRule(variant.Id, PriceRule.Create("Always", PriceAdjustmentType.FixedDiscount, 10m, 0));
        repository.Product.AddVariantPriceRule(variant.Id, PriceRule.Create("Scheduled", PriceAdjustmentType.PercentageDiscount, 25m, 5, Start, Start.AddHours(1)));
        using var source = new CancellationTokenSource();
        var at = Start.AddHours(hours);

        var result = await new UseCase(repository).ExecuteAsync(new(repository.Product.Id, variant.Id, at), source.Token);

        var quote = Assert.IsType<ProductVariantPriceQuote>(result.Quote);
        Assert.True(result.IsSuccess);
        Assert.Equal(Money.Create(amount, "EUR"), quote.Price);
        Assert.Equal(Money.Create(100m, "EUR"), quote.BasePrice);
        Assert.Equal(variant.PriceRules.Single(rule => rule.Name == (hours is 0 or 1 ? "Scheduled" : "Always")).Id,
            quote.AppliedPriceRuleId);
        Assert.Equal(at, quote.At);
        Assert.Equal(at.Offset, quote.At.Offset);
        Assert.Equal(repository.Token.Revision, quote.Revision);
        Assert.Equal(source.Token, repository.ReadCancellation);
        Assert.Equal(Money.Create(100m, "EUR"), variant.Price);
        Assert.Equal(2, variant.PriceRules.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_NoActiveRuleReturnsNullAppliedRuleId(bool expired)
    {
        var repository = new RepositoryFake();
        var variant = repository.Product.Variants.Single();
        repository.Product.SetVariantPrice(variant.Id, Money.Create(20m, "EUR"));
        if (expired)
            repository.Product.AddVariantPriceRule(variant.Id,
                PriceRule.Create("Past", PriceAdjustmentType.FixedDiscount, 5m, 0, null, Start.AddTicks(-1)));
        var result = await new UseCase(repository).ExecuteAsync(
            new(repository.Product.Id, variant.Id, Start), CancellationToken.None);
        Assert.Null(result.Quote!.AppliedPriceRuleId);
        Assert.Equal(result.Quote.BasePrice, result.Quote.Price);
    }

    [Fact]
    public async Task ExecuteAsync_AppliedRuleIsReportedEvenWhenZeroPriceDoesNotChange()
    {
        var repository = new RepositoryFake();
        var variant = repository.Product.Variants.Single();
        repository.Product.SetVariantPrice(variant.Id, Money.Create(0m, "EUR"));
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 5m, 0);
        repository.Product.AddVariantPriceRule(variant.Id, rule);
        var result = await new UseCase(repository).ExecuteAsync(
            new(repository.Product.Id, variant.Id, Start), CancellationToken.None);
        Assert.Equal(rule.Id, result.Quote!.AppliedPriceRuleId);
        Assert.Equal(0m, result.Quote.Price.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_UnpricedVariantHasSpecificFailure()
    {
        var repository = new RepositoryFake();
        var result = await new UseCase(repository).ExecuteAsync(
            new(repository.Product.Id, repository.Product.Variants.Single().Id, Start), CancellationToken.None);
        Assert.Equal(GetProductVariantPriceFailure.PriceNotSet, result.Failure);
        Assert.Null(result.Quote);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroPriceIsValidAndOnlyRequestedVariantIsCalculated()
    {
        var repository = new RepositoryFake();
        var second = repository.Product.AddVariant("Free");
        repository.Product.SetVariantPrice(second.Id, Money.Create(0m, "USD"));
        var result = await new UseCase(repository).ExecuteAsync(new(repository.Product.Id, second.Id, Start), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(Money.Create(0m, "USD"), result.Quote!.Price);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ReturnsSpecificMissingEntity(bool missingProduct)
    {
        var repository = new RepositoryFake { Missing = missingProduct };
        var result = await new UseCase(repository).ExecuteAsync(
            new(repository.Product.Id, ProductVariantId.New(), Start), CancellationToken.None);
        Assert.Equal(missingProduct ? GetProductVariantPriceFailure.ProductNotFound : GetProductVariantPriceFailure.VariantNotFound, result.Failure);
        Assert.Null(result.Quote);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_RejectsEmptyIdentityBeforeReading(bool emptyProduct)
    {
        var repository = new RepositoryFake();
        await Assert.ThrowsAsync<ArgumentException>(() => new UseCase(repository).ExecuteAsync(
            new(emptyProduct ? default : repository.Product.Id,
                emptyProduct ? repository.Product.Variants.Single().Id : default, Start), CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_HonorsCancellationBeforeReading()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new UseCase(repository).ExecuteAsync(
            new(repository.Product.Id, repository.Product.Variants.Single().Id, Start), source.Token));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    [Fact]
    public async Task ExecuteAsync_RejectsNullQuery() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => new UseCase(new RepositoryFake()).ExecuteAsync(null!, CancellationToken.None));

    private sealed class RepositoryFake : IProductRepository
    {
        public RepositoryFake() => Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "First");
        public ProductConcurrencyToken Token { get; }
        public bool Missing { get; init; }
        public int ReadCalls { get; private set; }
        public CancellationToken ReadCancellation { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Assert.Equal(Product.Id, id);
            cancellationToken.ThrowIfCancellationRequested();
            ReadCalls++;
            ReadCancellation = cancellationToken;
            return Task.FromResult(Missing ? null : new ProductSnapshot(Product, Token));
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only use case");
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only use case");
    }
}
