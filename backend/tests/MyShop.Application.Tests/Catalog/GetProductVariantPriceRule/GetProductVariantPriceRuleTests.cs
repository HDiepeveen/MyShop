using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductVariantPriceRule;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductVariantPriceRule.GetProductVariantPriceRule;

namespace MyShop.Application.Tests.Catalog.GetProductVariantPriceRule;

public sealed class GetProductVariantPriceRuleTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsRuleAndReadRevisionWithoutSaving()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        var result = await new UseCase(repository).ExecuteAsync(repository.Query, source.Token);
        Assert.True(result.IsSuccess);
        Assert.Same(repository.Rule, result.Snapshot!.Rule);
        Assert.Equal(repository.Token.Revision, result.Snapshot.Revision);
        Assert.Equal(source.Token, repository.ReadCancellation);
        Assert.Single(repository.Product.Variants.Single().PriceRules);
    }

    [Theory]
    [InlineData("product", GetProductVariantPriceRuleFailure.ProductNotFound)]
    [InlineData("variant", GetProductVariantPriceRuleFailure.VariantNotFound)]
    [InlineData("rule", GetProductVariantPriceRuleFailure.PriceRuleNotFound)]
    [InlineData("foreign", GetProductVariantPriceRuleFailure.PriceRuleNotFound)]
    public async Task ExecuteAsync_ReturnsSpecificFailureWithoutExposingForeignRule(
        string missing, GetProductVariantPriceRuleFailure failure)
    {
        var repository = new RepositoryFake { Missing = missing == "product" };
        var query = missing switch
        {
            "variant" => repository.Query with { ProductVariantId = ProductVariantId.New() },
            "rule" => repository.Query with { PriceRuleId = Guid.NewGuid() },
            "foreign" => repository.Query with { ProductVariantId = repository.Product.AddVariant("Second").Id },
            _ => repository.Query
        };
        var result = await new UseCase(repository).ExecuteAsync(query, CancellationToken.None);
        Assert.Equal(failure, result.Failure);
        Assert.Null(result.Snapshot);
    }

    [Theory]
    [InlineData("product")]
    [InlineData("variant")]
    [InlineData("rule")]
    public async Task ExecuteAsync_RejectsEmptyIdentityBeforeRead(string empty)
    {
        var repository = new RepositoryFake();
        var query = empty switch
        {
            "product" => repository.Query with { ProductId = default },
            "variant" => repository.Query with { ProductVariantId = default },
            _ => repository.Query with { PriceRuleId = Guid.Empty }
        };
        await Assert.ThrowsAsync<ArgumentException>(() => new UseCase(repository).ExecuteAsync(query, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_HonorsPreCanceledToken()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new UseCase(repository).ExecuteAsync(repository.Query, source.Token));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() => Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    [Fact]
    public async Task ExecuteAsync_RejectsNullQuery() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => new UseCase(new RepositoryFake()).ExecuteAsync(null!, CancellationToken.None));

    private sealed class RepositoryFake : IProductRepository
    {
        public RepositoryFake()
        {
            Product.AddVariantPriceRule(Product.Variants.Single().Id, Rule);
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
            Query = new(Product.Id, Product.Variants.Single().Id, Rule.Id);
        }
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "Variant");
        public PriceRule Rule { get; } = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 3);
        public GetProductVariantPriceRuleQuery Query { get; }
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
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
    }
}
