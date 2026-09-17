using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductAttributeValue.RemoveProductAttributeValue;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class RemoveProductAttributeValueEndpointTests
{
    [Fact]
    public void MapRemoveProductAttributeValue_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => RemoveProductAttributeValueEndpoint.MapRemoveProductAttributeValue(null!));

    [Fact]
    public void MapRemoveProductAttributeValue_MapsNamedDeleteRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapRemoveProductAttributeValue());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/attributes/{attributeDefinitionId:guid}",
            endpoint.RoutePattern.RawText);
        Assert.Equal("RemoveProductAttributeValue", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["DELETE"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_RemovesValueAndReturnsNoContent()
    {
        var scenario = new Scenario(withValue: true);

        var result = await scenario.ExecuteAsync();

        Assert.IsType<NoContent>(result.Result);
        Assert.Empty(scenario.Product.AttributeValues);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValueDoesNotExist_IsIdempotent()
    {
        var scenario = new Scenario();

        var result = await scenario.ExecuteAsync();

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var scenario = new Scenario { ProductIsMissing = true };

        var result = await scenario.ExecuteAsync();

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal("Product not found", notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExecuteAsync_WhenIdIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        bool emptyDefinitionId)
    {
        var scenario = new Scenario();

        var result = await RemoveProductAttributeValueEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyDefinitionId ? Guid.Empty : scenario.DefinitionId.Value,
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product attribute removal", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConflict()
    {
        var scenario = new Scenario(withValue: true) { HasConcurrencyConflict = true };

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

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.ExecuteAsync(source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => RemoveProductAttributeValueEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));

    private sealed class Scenario
    {
        public Scenario(bool withValue = false)
        {
            Product = Product.Create("Shirt", ProductTypeId.New(), "Medium");
            DefinitionId = AttributeDefinitionId.New();
            if (withValue)
                Product.SetAttributeValue(TextAttributeValue.Create(DefinitionId, "Cotton"));
            Products = new ProductRepositoryFake(this);
            UseCase = new UseCase(Products);
        }

        public Product Product { get; }
        public AttributeDefinitionId DefinitionId { get; }
        public bool ProductIsMissing { get; set; }
        public bool HasConcurrencyConflict { get; set; }
        public ProductRepositoryFake Products { get; }
        public UseCase UseCase { get; }

        public Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>>
            ExecuteAsync(CancellationToken cancellationToken = default) =>
            RemoveProductAttributeValueEndpoint.ExecuteAsync(
                Product.Id.Value,
                DefinitionId.Value,
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
            SaveCalls++;
            if (scenario.HasConcurrencyConflict)
                return Task.FromException<ProductConcurrencyToken>(new ProductConcurrencyException(product.Id));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }
}
