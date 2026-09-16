using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AddProductVariant.AddProductVariant;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class AddProductVariantEndpointTests
{
    [Fact]
    public void MapAddProductVariant_MapsNamedPostRouteAndReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var returned = app.MapAddProductVariant();

        Assert.Same(app, returned);
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal("/api/products/{productId:guid}/variants", endpoint.RoutePattern.RawText);
        Assert.Equal("AddProductVariant", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["POST"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_AddsVariantAndReturnsCreatedResponse()
    {
        var scenario = new Scenario();

        var result = await AddProductVariantEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            new AddProductVariantRequest("Large"),
            scenario.UseCase,
            CancellationToken.None);

        var created = Assert.IsType<Created<AddProductVariantResponse>>(result.Result);
        var response = Assert.IsType<AddProductVariantResponse>(created.Value);
        var variant = scenario.Product.Variants.Last();
        Assert.Equal($"/api/products/{scenario.Product.Id.Value}", created.Location);
        Assert.Equal(variant.Id.Value, response.Id);
        Assert.Equal("Large", response.Name);
        Assert.Equal(scenario.Products.ReturnedToken!.Revision, response.Revision);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var scenario = new Scenario { ProductIsMissing = true };

        var result = await AddProductVariantEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            new AddProductVariantRequest("Large"),
            scenario.UseCase,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal("Product not found", notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, "Large")]
    [InlineData(false, "")]
    [InlineData(false, " ")]
    public async Task ExecuteAsync_WhenRequestIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        string name)
    {
        var scenario = new Scenario();

        var result = await AddProductVariantEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            new AddProductVariantRequest(name),
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product variant", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConflict()
    {
        var scenario = new Scenario { HasConcurrencyConflict = true };

        var result = await AddProductVariantEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            new AddProductVariantRequest("Large"),
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
            AddProductVariantEndpoint.ExecuteAsync(
                scenario.Product.Id.Value,
                new AddProductVariantRequest("Large"),
                scenario.UseCase,
                source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Product.Create("Shirt", ProductTypeId.New(), "Medium");
            Products = new ProductRepositoryFake(this);
            UseCase = new UseCase(Products);
        }

        public Product Product { get; }
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
        public ProductConcurrencyToken? ReturnedToken { get; private set; }

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
            ReturnedToken = ProductConcurrencyToken.Create(product.Id, Guid.NewGuid());
            return Task.FromResult(ReturnedToken);
        }
    }
}
