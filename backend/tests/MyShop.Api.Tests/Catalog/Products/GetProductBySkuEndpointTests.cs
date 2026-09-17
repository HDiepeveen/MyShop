using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductBySku.GetProductBySku;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class GetProductBySkuEndpointTests
{
    [Fact]
    public void MapGetProductBySku_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => GetProductBySkuEndpoint.MapGetProductBySku(null!));

    [Fact]
    public void MapGetProductBySku_MapsNamedGetRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapGetProductBySku());
        var endpoint = Assert.IsType<RouteEndpoint>(
            Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/products/by-sku/{sku}", endpoint.RoutePattern.RawText);
        Assert.Equal("GetProductBySku", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSkuExists_ReturnsOwner()
    {
        var owner = new ProductSkuOwner(ProductId.New(), ProductVariantId.New());
        var useCase = new UseCase(new ProductSkuLookupFake(owner));

        var result = await GetProductBySkuEndpoint.ExecuteAsync("sku-1", useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<ProductSkuOwnerResponse>>(result.Result);
        Assert.Equal(owner.ProductId.Value, ok.Value!.ProductId);
        Assert.Equal(owner.ProductVariantId.Value, ok.Value.ProductVariantId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSkuDoesNotExist_ReturnsNotFound()
    {
        var useCase = new UseCase(new ProductSkuLookupFake(null));

        var result = await GetProductBySkuEndpoint.ExecuteAsync("missing", useCase, CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal("SKU not found", notFound.Value!.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExecuteAsync_WhenSkuIsInvalid_ReturnsBadRequest(string sku)
    {
        var lookup = new ProductSkuLookupFake(null);
        var useCase = new UseCase(lookup);

        var result = await GetProductBySkuEndpoint.ExecuteAsync(sku, useCase, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid SKU", badRequest.Value!.Title);
        Assert.Equal(0, lookup.FindCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var lookup = new ProductSkuLookupFake(null);
        var useCase = new UseCase(lookup);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GetProductBySkuEndpoint.ExecuteAsync("sku", useCase, source.Token));

        Assert.Equal(0, lookup.FindCalls);
    }

    private sealed class ProductSkuLookupFake(ProductSkuOwner? owner) : IProductSkuLookup
    {
        public int FindCalls { get; private set; }

        public Task<ProductSkuOwner?> FindOwnerAsync(Sku sku, CancellationToken cancellationToken)
        {
            FindCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(owner);
        }
    }
}
