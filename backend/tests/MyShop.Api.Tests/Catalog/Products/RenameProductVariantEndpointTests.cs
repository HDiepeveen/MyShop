using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductVariant.RenameProductVariant;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class RenameProductVariantEndpointTests
{
    [Fact]
    public void MapRenameProductVariant_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => RenameProductVariantEndpoint.MapRenameProductVariant(null!));

    [Fact]
    public void MapRenameProductVariant_MapsNamedPatchRouteAndReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var returned = app.MapRenameProductVariant();

        Assert.Same(app, returned);
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/variants/{variantId:guid}/name",
            endpoint.RoutePattern.RawText);
        Assert.Equal("RenameProductVariant", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PATCH"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_RenamesVariantAndReturnsNoContent()
    {
        var scenario = new Scenario();

        var result = await RenameProductVariantEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new RenameProductVariantRequest("Large"),
            scenario.UseCase,
            CancellationToken.None);

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal("Large", scenario.Variant.Name);
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

        var result = await RenameProductVariantEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            variantId.Value,
            new RenameProductVariantRequest("Large"),
            scenario.UseCase,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(
            productIsMissing ? "Product not found" : "Product variant not found",
            notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, false, "Large")]
    [InlineData(false, true, "Large")]
    [InlineData(false, false, "")]
    [InlineData(false, false, " ")]
    public async Task ExecuteAsync_WhenRequestIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        bool emptyVariantId,
        string name)
    {
        var scenario = new Scenario();

        var result = await RenameProductVariantEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyVariantId ? Guid.Empty : scenario.Variant.Id.Value,
            new RenameProductVariantRequest(name),
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product variant rename", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConflict()
    {
        var scenario = new Scenario { HasConcurrencyConflict = true };

        var result = await RenameProductVariantEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.Variant.Id.Value,
            new RenameProductVariantRequest("Large"),
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
            RenameProductVariantEndpoint.ExecuteAsync(
                scenario.Product.Id.Value,
                scenario.Variant.Id.Value,
                new RenameProductVariantRequest("Large"),
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
}
