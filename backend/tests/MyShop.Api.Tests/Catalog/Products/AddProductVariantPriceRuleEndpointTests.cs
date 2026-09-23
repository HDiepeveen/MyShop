using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AddProductVariantPriceRule.AddProductVariantPriceRule;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class AddProductVariantPriceRuleEndpointTests
{
    [Fact]
    public void MapAddProductVariantPriceRule_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => AddProductVariantPriceRuleEndpoint.MapAddProductVariantPriceRule(null!));

    [Fact]
    public void MapAddProductVariantPriceRule_MapsNamedPostRouteAndReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var returned = app.MapAddProductVariantPriceRule();

        Assert.Same(app, returned);
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules",
            endpoint.RoutePattern.RawText);
        Assert.Equal("AddProductVariantPriceRule", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["POST"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_AddsRuleAndReturnsCreatedWithIdentity()
    {
        var scenario = new Scenario();

        var result = await AddProductVariantPriceRuleEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new AddProductVariantPriceRuleRequest(" Sale ", 1, 10m, 5),
            scenario.UseCase,
            CancellationToken.None);

        var created = Assert.IsType<Created<AddProductVariantPriceRuleResponse>>(result.Result);
        var rule = Assert.Single(scenario.Variant.PriceRules);
        Assert.Equal(rule.Id, created.Value!.Id);
        Assert.Equal("Sale", rule.Name);
        Assert.Equal(10m, rule.Value);
        Assert.Equal(5, rule.Priority);
        Assert.Equal(PriceAdjustmentType.PercentageDiscount, rule.AdjustmentType);
        Assert.Equal($"/api/products/{scenario.Product.Id.Value}/variants/{scenario.Variant.Id.Value}/price-rules/{rule.Id}", created.Location);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WhenProductOrVariantDoesNotExist_ReturnsSpecificNotFound(
        bool productIsMissing)
    {
        var scenario = new Scenario { ProductIsMissing = productIsMissing };
        var variantId = productIsMissing ? scenario.Variant.Id : ProductVariantId.New();

        var result = await AddProductVariantPriceRuleEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            variantId.Value,
            new AddProductVariantPriceRuleRequest("Sale", 1, 10m, 5),
            scenario.UseCase,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(
            productIsMissing ? "Product not found" : "Product variant not found",
            notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData("product")]
    [InlineData("variant")]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("value")]
    [InlineData("period")]
    [InlineData("long name")]
    [InlineData("large value")]
    public async Task ExecuteAsync_InvalidRequestReturnsBadRequestWithoutSave(string invalidField)
    {
        var scenario = new Scenario();
        var at = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var request = new AddProductVariantPriceRuleRequest("Sale", 1, 10m, 5);
        request = invalidField switch
        {
            "long name" => request with { Name = new string('x', 201) },
            "large value" => request with { AdjustmentType = 2, Value = 10000000000000000m },
            "name" => request with { Name = " " },
            "type" => request with { AdjustmentType = 99 },
            "value" => request with { Value = 101m },
            "period" => request with { StartsAt = at, EndsAt = at.AddDays(-1) },
            _ => request
        };
        var result = await AddProductVariantPriceRuleEndpoint.ExecuteAsync(
            invalidField == "product" ? Guid.Empty : scenario.Product.Id.Value,
            invalidField == "variant" ? Guid.Empty : scenario.Variant.Id.Value,
            request, scenario.UseCase, CancellationToken.None);
        Assert.Equal("Invalid product variant price rule",
            Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
        Assert.Empty(scenario.Variant.PriceRules);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConcurrencyConflict()
    {
        var scenario = new Scenario { HasConcurrencyConflict = true };

        var result = await AddProductVariantPriceRuleEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new AddProductVariantPriceRuleRequest("Sale", 1, 10m, 5),
            scenario.UseCase,
            CancellationToken.None);

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

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AddProductVariantPriceRuleEndpoint.ExecuteAsync(
                scenario.Product.Id.Value,
                scenario.Variant.Id.Value,
                new AddProductVariantPriceRuleRequest("Sale", 1, 10m, 5),
                scenario.UseCase,
                source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => AddProductVariantPriceRuleEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, null!, CancellationToken.None));

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => AddProductVariantPriceRuleEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new("Sale", 1, 10m, 5), null!, CancellationToken.None));

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Product.Create("Shirt", ProductTypeId.New(), "Medium");
            Variant = Product.Variants.Single();
            Products = new ProductRepositoryFake(this);
            UseCase = new UseCase(Products);
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public bool ProductIsMissing { get; set; }
        public bool HasConcurrencyConflict { get; set; }
        public ProductRepositoryFake Products { get; }
        public UseCase UseCase { get; }
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        private readonly ProductConcurrencyToken _readToken =
            ProductConcurrencyToken.Create(scenario.Product.Id, Guid.NewGuid());

        public int SaveCalls { get; private set; }
        public CancellationToken ReadCancellationToken { get; private set; }
        public CancellationToken SaveCancellationToken { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(
            ProductId id,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(scenario.Product.Id, id);
            ReadCancellationToken = cancellationToken;
            return Task.FromResult(scenario.ProductIsMissing
                ? null
                : new ProductSnapshot(scenario.Product, _readToken));
        }

        public Task<ProductConcurrencyToken> AddAsync(
            Product product,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken)
        {
            Assert.Same(scenario.Product, product);
            Assert.Equal(_readToken, expectedToken);
            SaveCancellationToken = cancellationToken;
            SaveCalls++;
            if (scenario.HasConcurrencyConflict)
                return Task.FromException<ProductConcurrencyToken>(
                    new ProductConcurrencyException(product.Id));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }

}
