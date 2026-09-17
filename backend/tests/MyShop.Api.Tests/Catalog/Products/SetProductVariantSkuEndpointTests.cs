using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetProductVariantSku.SetProductVariantSku;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class SetProductVariantSkuEndpointTests
{
    [Fact]
    public void MapSetProductVariantSku_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => SetProductVariantSkuEndpoint.MapSetProductVariantSku(null!));

    [Fact]
    public void MapSetProductVariantSku_MapsNamedPutRouteAndReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var returned = app.MapSetProductVariantSku();

        Assert.Same(app, returned);
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/variants/{variantId:guid}/sku",
            endpoint.RoutePattern.RawText);
        Assert.Equal("SetProductVariantSku", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PUT"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_SetsCanonicalSkuAndReturnsNoContent()
    {
        var scenario = new Scenario();

        var result = await SetProductVariantSkuEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new SetProductVariantSkuRequest("sku-1"),
            scenario.UseCase,
            CancellationToken.None);

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal("SKU-1", scenario.Variant.Sku!.Value);
        Assert.Equal("SKU-1", scenario.Lookup.RequestedSku!.Value);
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

        var result = await SetProductVariantSkuEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            variantId.Value,
            new SetProductVariantSkuRequest("SKU-1"),
            scenario.UseCase,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(
            productIsMissing ? "Product not found" : "Product variant not found",
            notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSkuIsAlreadyInUse_ReturnsConflict()
    {
        var scenario = new Scenario();
        var owner = Product.Create("Owner", ProductTypeId.New(), "Standard");
        scenario.Lookup.Owner = new ProductSkuOwner(owner.Id, owner.Variants.Single().Id);

        var result = await SetProductVariantSkuEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new SetProductVariantSkuRequest("DUPLICATE"),
            scenario.UseCase,
            CancellationToken.None);

        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("SKU already in use", conflict.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, false, "SKU-1")]
    [InlineData(false, true, "SKU-1")]
    [InlineData(false, false, "")]
    [InlineData(false, false, "HAS SPACE")]
    public async Task ExecuteAsync_WhenRequestIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        bool emptyVariantId,
        string sku)
    {
        var scenario = new Scenario();

        var result = await SetProductVariantSkuEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyVariantId ? Guid.Empty : scenario.Variant.Id.Value,
            new SetProductVariantSkuRequest(sku),
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product variant SKU", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConcurrencyConflict()
    {
        var scenario = new Scenario { HasConcurrencyConflict = true };

        var result = await SetProductVariantSkuEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new SetProductVariantSkuRequest("SKU-1"),
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
            SetProductVariantSkuEndpoint.ExecuteAsync(
                scenario.Product.Id.Value,
                scenario.Variant.Id.Value,
                new SetProductVariantSkuRequest("SKU-1"),
                scenario.UseCase,
                source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Product.Create("Shirt", ProductTypeId.New(), "Medium");
            Variant = Product.Variants.Single();
            Products = new ProductRepositoryFake(this);
            Lookup = new ProductSkuLookupFake();
            UseCase = new UseCase(Products, Lookup);
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public bool ProductIsMissing { get; set; }
        public bool HasConcurrencyConflict { get; set; }
        public ProductRepositoryFake Products { get; }
        public ProductSkuLookupFake Lookup { get; }
        public UseCase UseCase { get; }
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        private readonly ProductConcurrencyToken _readToken =
            ProductConcurrencyToken.Create(scenario.Product.Id, Guid.NewGuid());

        public int SaveCalls { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(
            ProductId id,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            SaveCalls++;
            if (scenario.HasConcurrencyConflict)
                return Task.FromException<ProductConcurrencyToken>(
                    new ProductConcurrencyException(product.Id));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }

    private sealed class ProductSkuLookupFake : IProductSkuLookup
    {
        public ProductSkuOwner? Owner { get; set; }
        public Sku? RequestedSku { get; private set; }

        public Task<ProductSkuOwner?> FindOwnerAsync(
            Sku sku,
            CancellationToken cancellationToken)
        {
            RequestedSku = sku;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Owner);
        }
    }
}
