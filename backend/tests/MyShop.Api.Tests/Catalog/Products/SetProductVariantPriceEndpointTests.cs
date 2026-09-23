using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetProductVariantPrice.SetProductVariantPrice;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class SetProductVariantPriceEndpointTests
{
    [Fact]
    public void MapSetProductVariantPrice_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => SetProductVariantPriceEndpoint.MapSetProductVariantPrice(null!));

    [Fact]
    public void MapSetProductVariantPrice_MapsNamedPutRouteAndReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var returned = app.MapSetProductVariantPrice();

        Assert.Same(app, returned);
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/variants/{variantId:guid}/price",
            endpoint.RoutePattern.RawText);
        Assert.Equal("SetProductVariantPrice", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PUT"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_SetsNormalizedPriceAndReturnsNoContent()
    {
        var scenario = new Scenario();

        var result = await SetProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new SetProductVariantPriceRequest(12.345m, " eur "),
            scenario.UseCase,
            CancellationToken.None);

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(Money.Create(12.34m, "EUR"), scenario.Variant.Price);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNormalizedPriceIsUnchanged_DoesNotSave()
    {
        var scenario = new Scenario();
        scenario.Product.SetVariantPrice(scenario.Variant.Id, Money.Create(12.34m, "EUR"));

        var result = await SetProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.Variant.Id.Value,
            new(12.345m, " eur "), scenario.UseCase, CancellationToken.None);

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_AllowsZeroPriceAndForwardsTokenToReadAndSave()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        var result = await SetProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.Variant.Id.Value,
            new(0m, "EUR"), scenario.UseCase, source.Token);

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(Money.Create(0m, "EUR"), scenario.Variant.Price);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Equal(source.Token, scenario.Products.ReadCancellationToken);
        Assert.Equal(source.Token, scenario.Products.SaveCancellationToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WhenProductOrVariantDoesNotExist_ReturnsSpecificNotFound(
        bool productIsMissing)
    {
        var scenario = new Scenario { ProductIsMissing = productIsMissing };
        var variantId = productIsMissing ? scenario.Variant.Id : ProductVariantId.New();

        var result = await SetProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            variantId.Value,
            new SetProductVariantPriceRequest(12.34m, "EUR"),
            scenario.UseCase,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(
            productIsMissing ? "Product not found" : "Product variant not found",
            notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, false, 10, "EUR")]
    [InlineData(false, true, 10, "EUR")]
    [InlineData(false, false, -1, "EUR")]
    [InlineData(false, false, 10, null)]
    [InlineData(false, false, 10, "")]
    [InlineData(false, false, 10, "EURO")]
    [InlineData(false, false, 10, "E1R")]
    public async Task ExecuteAsync_WhenRequestIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        bool emptyVariantId,
        int amount,
        string? currency)
    {
        var scenario = new Scenario();

        var result = await SetProductVariantPriceEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyVariantId ? Guid.Empty : scenario.Variant.Id.Value,
            new SetProductVariantPriceRequest(amount, currency!),
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product variant price", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConcurrencyConflict()
    {
        var scenario = new Scenario { HasConcurrencyConflict = true };

        var result = await SetProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new SetProductVariantPriceRequest(12.34m, "EUR"),
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
            SetProductVariantPriceEndpoint.ExecuteAsync(
                scenario.Product.Id.Value,
                scenario.Variant.Id.Value,
                new SetProductVariantPriceRequest(12.34m, "EUR"),
                scenario.UseCase,
                source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => SetProductVariantPriceEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, null!, CancellationToken.None));

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => SetProductVariantPriceEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new(12.34m, "EUR"), null!, CancellationToken.None));

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
