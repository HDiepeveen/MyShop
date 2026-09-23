using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductVariant;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductVariant.GetProductVariant;

namespace MyShop.Application.Tests.Catalog.GetProductVariant;

public sealed class GetProductVariantTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyRequestedVariantWithReadRevision()
    {
        var repository = new RepositoryFake();
        var first = repository.Product.Variants.Single();
        var second = repository.Product.AddVariant("Second");
        repository.Product.SetVariantSku(second.Id, Sku.Create("SECOND"));
        repository.Product.SetVariantPrice(second.Id, Money.Create(25m, "EUR"));
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 1);
        repository.Product.AddVariantPriceRule(second.Id, rule);
        using var source = new CancellationTokenSource();

        var result = await new UseCase(repository).ExecuteAsync(
            new(repository.Product.Id, second.Id), source.Token);

        Assert.True(result.IsSuccess);
        Assert.Same(second, result.Snapshot!.Variant);
        Assert.NotSame(first, result.Snapshot.Variant);
        Assert.Equal("SECOND", result.Snapshot.Variant.Sku!.Value);
        Assert.Equal(Money.Create(25m, "EUR"), result.Snapshot.Variant.Price);
        Assert.Same(rule, Assert.Single(result.Snapshot.Variant.PriceRules));
        Assert.Equal(repository.Token.Revision, result.Snapshot.Revision);
        Assert.Equal(source.Token, repository.ReadCancellation);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_MissingProductOrVariantHasSpecificFailure(bool missingProduct)
    {
        var repository = new RepositoryFake { Missing = missingProduct };
        var result = await new UseCase(repository).ExecuteAsync(
            repository.Query with { ProductVariantId = ProductVariantId.New() }, CancellationToken.None);
        Assert.Equal(missingProduct ? GetProductVariantFailure.ProductNotFound :
            GetProductVariantFailure.VariantNotFound, result.Failure);
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
        public GetProductVariantQuery Query { get; }
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
