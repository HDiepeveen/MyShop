using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductVariant.GetProductVariant;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class GetProductVariantEndpointTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsVariantDetailsMatchingProductDetails()
    {
        var repository = new RepositoryFake();
        var first = repository.Product.Variants.Single();
        var variant = repository.Product.AddVariant("Second");
        repository.Product.SetVariantSku(variant.Id, Sku.Create("SECOND"));
        repository.Product.SetVariantPrice(variant.Id, Money.Create(20m, "EUR"));
        var attributeId = AttributeDefinitionId.New();
        repository.Product.SetVariantAttributeValue(variant.Id, TextAttributeValue.Create(attributeId, "Red"));
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 0);
        repository.Product.AddVariantPriceRule(variant.Id, rule);

        var result = await GetProductVariantEndpoint.ExecuteAsync(
            repository.Product.Id.Value, variant.Id.Value, new UseCase(repository), CancellationToken.None);

        var response = Assert.IsType<Ok<GetProductVariantResponse>>(result.Result).Value!;
        Assert.Equal(variant.Id.Value, response.Variant.Id);
        Assert.NotEqual(first.Id.Value, response.Variant.Id);
        Assert.Equal("Second", response.Variant.Name);
        Assert.Equal("SECOND", response.Variant.Sku);
        Assert.Equal(new MoneyResponse(20m, "EUR"), response.Variant.Price);
        var attribute = Assert.Single(response.Variant.AttributeValues);
        Assert.Equal(attributeId.Value, attribute.AttributeDefinitionId);
        Assert.Equal("Red", attribute.Value);
        Assert.Equal(rule.Id, Assert.Single(response.Variant.PriceRules).Id);
        Assert.Equal(repository.Token.Revision, response.Revision);

        var productResult = await GetProductEndpoint.ExecuteAsync(repository.Product.Id.Value,
            new MyShop.Application.Catalog.GetProduct.GetProduct(repository), CancellationToken.None);
        var embedded = Assert.IsType<Ok<GetProductResponse>>(productResult.Result).Value!.Variants
            .Single(candidate => candidate.Id == variant.Id.Value);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(embedded),
            System.Text.Json.JsonSerializer.Serialize(response.Variant));
    }

    [Fact]
    public async Task ExecuteAsync_AbsentOptionalDataRemainsAbsent()
    {
        var repository = new RepositoryFake();
        var result = await GetProductVariantEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.Product.Variants.Single().Id.Value,
            new UseCase(repository), CancellationToken.None);
        var variant = Assert.IsType<Ok<GetProductVariantResponse>>(result.Result).Value!.Variant;
        Assert.Null(variant.Sku);
        Assert.Null(variant.Price);
        Assert.Empty(variant.PriceRules);
        Assert.Empty(variant.AttributeValues);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ReturnsSpecificNotFound(bool missingProduct)
    {
        var repository = new RepositoryFake { Missing = missingProduct };
        var result = await GetProductVariantEndpoint.ExecuteAsync(
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
        var result = await GetProductVariantEndpoint.ExecuteAsync(
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
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetProductVariantEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.Product.Variants.Single().Id.Value, new UseCase(repository), source.Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetProductVariantEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public void Map_RejectsNullBuilder() =>
        Assert.Throws<ArgumentNullException>(() => GetProductVariantEndpoint.MapGetProductVariant(null!));

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
