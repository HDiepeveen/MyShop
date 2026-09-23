using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductVariantPriceRule.RemoveProductVariantPriceRule;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class RemoveProductVariantPriceRuleEndpointTests
{
    [Fact]
    public void MapRemoveProductVariantPriceRule_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => RemoveProductVariantPriceRuleEndpoint.MapRemoveProductVariantPriceRule(null!));

    [Fact]
    public void MapRemoveProductVariantPriceRule_MapsNamedDeleteRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapRemoveProductVariantPriceRule());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules/{priceRuleId:guid}",
            endpoint.RoutePattern.RawText);
        Assert.Equal("RemoveProductVariantPriceRule", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["DELETE"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_RemovesRuleAndReturnsNoContent()
    {
        var scenario = new Scenario(withRule: true);

        var result = await scenario.ExecuteAsync();

        Assert.IsType<NoContent>(result.Result);
        Assert.Empty(scenario.Variant.PriceRules);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRuleDoesNotExist_IsIdempotent()
    {
        var scenario = new Scenario();

        var result = await scenario.ExecuteAsync();

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WhenProductOrVariantDoesNotExist_ReturnsSpecificNotFound(bool productIsMissing)
    {
        var scenario = new Scenario { ProductIsMissing = productIsMissing };
        var variantId = productIsMissing ? scenario.Variant.Id : ProductVariantId.New();

        var result = await scenario.ExecuteAsync(variantId: variantId);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(productIsMissing ? "Product not found" : "Product variant not found", notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task ExecuteAsync_WhenIdIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        bool emptyVariantId,
        bool emptyRuleId)
    {
        var scenario = new Scenario();

        var result = await RemoveProductVariantPriceRuleEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyVariantId ? Guid.Empty : scenario.Variant.Id.Value,
            emptyRuleId ? Guid.Empty : scenario.RuleId,
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product variant price rule removal", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConflict()
    {
        var scenario = new Scenario(withRule: true) { HasConcurrencyConflict = true };

        var result = await scenario.ExecuteAsync();

        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("Product was modified", conflict.Value!.Title);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scenario.ExecuteAsync(cancellationToken: source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => RemoveProductVariantPriceRuleEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));

    private sealed class Scenario
    {
        public Scenario(bool withRule = false)
        {
            Product = Product.Create("Shirt", ProductTypeId.New(), "Medium");
            Variant = Product.Variants.Single();
            var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 5m, 1);
            RuleId = rule.Id;
            if (withRule)
                Product.AddVariantPriceRule(Variant.Id, rule);
            Products = new ProductRepositoryFake(this);
            UseCase = new UseCase(Products);
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public Guid RuleId { get; }
        public bool ProductIsMissing { get; set; }
        public bool HasConcurrencyConflict { get; set; }
        public ProductRepositoryFake Products { get; }
        public UseCase UseCase { get; }

        public Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>>
            ExecuteAsync(
                ProductVariantId? variantId = null,
                CancellationToken cancellationToken = default) =>
            RemoveProductVariantPriceRuleEndpoint.ExecuteAsync(
                Product.Id.Value,
                (variantId ?? Variant.Id).Value,
                RuleId,
                UseCase,
                cancellationToken);
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        private readonly ProductConcurrencyToken _readToken =
            ProductConcurrencyToken.Create(scenario.Product.Id, Guid.NewGuid());

        public int SaveCalls { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.ProductIsMissing
                ? null
                : new ProductSnapshot(scenario.Product, _readToken));
        }

        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken)
        {
            Assert.Same(_readToken, expectedToken);
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            if (scenario.HasConcurrencyConflict)
                return Task.FromException<ProductConcurrencyToken>(new ProductConcurrencyException(product.Id));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }
}
