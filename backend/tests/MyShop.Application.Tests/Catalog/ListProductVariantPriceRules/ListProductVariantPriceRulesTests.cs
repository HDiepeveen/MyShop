using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ListProductVariantPriceRules;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListProductVariantPriceRules.ListProductVariantPriceRules;

namespace MyShop.Application.Tests.Catalog.ListProductVariantPriceRules;

public sealed class ListProductVariantPriceRulesTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsAllRulesInStableOrderWithoutOtherVariantsOrWrites()
    {
        var repository = new RepositoryFake();
        var variant = repository.Product.Variants.Single();
        var at = DateTimeOffset.Parse("2026-01-01T00:00:00+02:00");
        var low = PriceRule.Create("Low", PriceAdjustmentType.FixedDiscount, 2m, -1);
        var high = PriceRule.Create("High", PriceAdjustmentType.PercentageDiscount, 25m, 5, at, at);
        var tied = PriceRule.Create("Tied", PriceAdjustmentType.FixedDiscount, 3m, 5);
        foreach (var rule in new[] { low, high, tied }) repository.Product.AddVariantPriceRule(variant.Id, rule);
        var second = repository.Product.AddVariant("Other");
        repository.Product.AddVariantPriceRule(second.Id, PriceRule.Create("Other", PriceAdjustmentType.FixedDiscount, 4m, 9));
        using var cancellation = new CancellationTokenSource();

        var result = await new UseCase(repository).ExecuteAsync(
            new(repository.Product.Id, variant.Id), cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { high.Id, tied.Id }.Order().Append(low.Id), result.Snapshot!.Rules.Select(rule => rule.Id));
        Assert.Same(high, result.Snapshot.Rules.Single(rule => rule.Id == high.Id));
        Assert.Equal(repository.Token.Revision, result.Snapshot.Revision);
        Assert.Equal(cancellation.Token, repository.ReadCancellation);
        Assert.Equal(new[] { low, high, tied }, variant.PriceRules);
        repository.Product.RemoveVariantPriceRule(variant.Id, high.Id);
        Assert.Equal(3, result.Snapshot.Rules.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<PriceRule>)result.Snapshot.Rules).Clear());
    }

    [Fact]
    public async Task ExecuteAsync_EmptyListIsSuccessful()
    {
        var repository = new RepositoryFake();
        var result = await new UseCase(repository).ExecuteAsync(repository.Query, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Snapshot!.Rules);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_MissingProductOrVariantHasSpecificFailure(bool missingProduct)
    {
        var repository = new RepositoryFake { Missing = missingProduct };
        var result = await new UseCase(repository).ExecuteAsync(
            repository.Query with { ProductVariantId = ProductVariantId.New() }, CancellationToken.None);
        Assert.Equal(missingProduct ? ListProductVariantPriceRulesFailure.ProductNotFound :
            ListProductVariantPriceRulesFailure.VariantNotFound, result.Failure);
        Assert.Null(result.Snapshot);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_RejectsEmptyIdsBeforeRead(bool product)
    {
        var repository = new RepositoryFake();
        await Assert.ThrowsAsync<ArgumentException>(() => new UseCase(repository).ExecuteAsync(
            product ? repository.Query with { ProductId = default } :
                repository.Query with { ProductVariantId = default }, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsCanceledAndNullQueriesBeforeRead()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UseCase(repository).ExecuteAsync(repository.Query, source.Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new UseCase(repository).ExecuteAsync(null!, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() => Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    private sealed class RepositoryFake : IProductRepository
    {
        public RepositoryFake()
        {
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
            Query = new(Product.Id, Product.Variants.Single().Id);
        }
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "First");
        public ProductConcurrencyToken Token { get; }
        public ListProductVariantPriceRulesQuery Query { get; }
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
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
    }
}
