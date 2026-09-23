using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListProductVariantPriceRules.ListProductVariantPriceRules;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class ListProductVariantPriceRulesEndpointTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsOrderedCompleteRulesAndRevision()
    {
        var repository = new RepositoryFake();
        var variant = repository.Product.Variants.Single();
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00+02:00");
        var low = PriceRule.Create("Low", PriceAdjustmentType.FixedDiscount, 2m, 0);
        var high = PriceRule.Create("Scheduled", PriceAdjustmentType.PercentageDiscount, 25m, 5, start, start.AddDays(1));
        repository.Product.AddVariantPriceRule(variant.Id, low);
        repository.Product.AddVariantPriceRule(variant.Id, high);
        var other = repository.Product.AddVariant("Other");
        repository.Product.AddVariantPriceRule(other.Id, PriceRule.Create("Hidden", PriceAdjustmentType.FixedDiscount, 9m, 9));

        var result = await ListProductVariantPriceRulesEndpoint.ExecuteAsync(
            repository.Product.Id.Value, variant.Id.Value, new UseCase(repository), CancellationToken.None);

        var response = Assert.IsType<Ok<ProductVariantPriceRulesResponse>>(result.Result).Value!;
        Assert.Equal(new[] { high.Id, low.Id }, response.Rules.Select(rule => rule.Id));
        Assert.Equal(new PriceRuleResponse(high.Id, "Scheduled", 1, 25m, 5, start, start.AddDays(1)), response.Rules[0]);
        Assert.Equal(start.Offset, response.Rules[0].StartsAt!.Value.Offset);
        Assert.Equal(repository.Token.Revision, response.Revision);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessfulEmptyCollection()
    {
        var repository = new RepositoryFake();
        var result = await ListProductVariantPriceRulesEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.Product.Variants.Single().Id.Value,
            new UseCase(repository), CancellationToken.None);
        Assert.Empty(Assert.IsType<Ok<ProductVariantPriceRulesResponse>>(result.Result).Value!.Rules);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ReturnsSpecificNotFound(bool missingProduct)
    {
        var repository = new RepositoryFake { Missing = missingProduct };
        var result = await ListProductVariantPriceRulesEndpoint.ExecuteAsync(
            repository.Product.Id.Value, Guid.NewGuid(), new UseCase(repository), CancellationToken.None);
        Assert.Equal(missingProduct ? "Product not found" : "Product variant not found",
            Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_RejectsEmptyIdsBeforeRead(bool product)
    {
        var repository = new RepositoryFake();
        var result = await ListProductVariantPriceRulesEndpoint.ExecuteAsync(
            product ? Guid.Empty : repository.Product.Id.Value,
            product ? repository.Product.Variants.Single().Id.Value : Guid.Empty,
            new UseCase(repository), CancellationToken.None);
        Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullDependencyAndCancellation()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ListProductVariantPriceRulesEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.Product.Variants.Single().Id.Value, new UseCase(repository), source.Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ListProductVariantPriceRulesEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public void Map_RejectsNullBuilder() =>
        Assert.Throws<ArgumentNullException>(() => ListProductVariantPriceRulesEndpoint.MapListProductVariantPriceRules(null!));

    private sealed class RepositoryFake : IProductRepository
    {
        public RepositoryFake() => Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "Variant");
        public ProductConcurrencyToken Token { get; }
        public bool Missing { get; init; }
        public int ReadCalls { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Assert.Equal(Product.Id, id);
            cancellationToken.ThrowIfCancellationRequested();
            ReadCalls++;
            return Task.FromResult(Missing ? null : new ProductSnapshot(Product, Token));
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
    }
}
