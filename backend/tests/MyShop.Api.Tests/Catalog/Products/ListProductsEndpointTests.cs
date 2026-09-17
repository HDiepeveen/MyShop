using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListProducts.ListProducts;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class ListProductsEndpointTests
{
    [Fact]
    public void MapListProducts_MapsNamedGetRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapListProducts());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal("/api/products", endpoint.RoutePattern.RawText);
        Assert.Equal("ListProducts", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_MapsPageAndPagingMetadata()
    {
        var item = new ProductListItem(Guid.NewGuid(), Guid.NewGuid(), "Shirt", 2);
        var repository = new ProductListRepositoryFake(new ProductListPage([item], 12));
        var useCase = new UseCase(repository);

        var result = await ListProductsEndpoint.ExecuteAsync(5, 10, null, useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<ProductListResponse>>(result.Result);
        var response = Assert.IsType<ProductListResponse>(ok.Value);
        Assert.Equal(5, response.Offset);
        Assert.Equal(10, response.Limit);
        Assert.Equal(12, response.TotalCount);
        var mapped = Assert.Single(response.Items);
        Assert.Equal(item.Id, mapped.Id);
        Assert.Equal(item.ProductTypeId, mapped.ProductTypeId);
        Assert.Equal(item.Name, mapped.Name);
        Assert.Equal(item.VariantCount, mapped.VariantCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPagingIsOmitted_UsesDefaults()
    {
        var repository = new ProductListRepositoryFake(new ProductListPage([], 0));
        var useCase = new UseCase(repository);

        var result = await ListProductsEndpoint.ExecuteAsync(null, null, null, useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<ProductListResponse>>(result.Result);
        Assert.Equal(0, ok.Value!.Offset);
        Assert.Equal(UseCase.DefaultLimit, ok.Value.Limit);
        Assert.Equal(0, repository.Offset);
        Assert.Equal(UseCase.DefaultLimit, repository.Limit);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public async Task ExecuteAsync_WhenPagingIsInvalid_ReturnsBadRequest(int offset, int limit)
    {
        var repository = new ProductListRepositoryFake(new ProductListPage([], 0));
        var useCase = new UseCase(repository);

        var result = await ListProductsEndpoint.ExecuteAsync(offset, limit, null, useCase, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product paging", badRequest.Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var repository = new ProductListRepositoryFake(new ProductListPage([], 0));
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ListProductsEndpoint.ExecuteAsync(null, null, null, useCase, source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_FiltersByProductType()
    {
        var repository = new ProductListRepositoryFake(new ProductListPage([], 0));
        var useCase = new UseCase(repository);
        var productTypeId = Guid.NewGuid();

        var result = await ListProductsEndpoint.ExecuteAsync(
            null, null, productTypeId, useCase, CancellationToken.None);

        Assert.IsType<Ok<ProductListResponse>>(result.Result);
        Assert.Equal(ProductTypeId.From(productTypeId), repository.ProductTypeId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductTypeIdIsEmpty_ReturnsBadRequest()
    {
        var repository = new ProductListRepositoryFake(new ProductListPage([], 0));
        var useCase = new UseCase(repository);

        var result = await ListProductsEndpoint.ExecuteAsync(
            null, null, Guid.Empty, useCase, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product type filter", badRequest.Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    private sealed class ProductListRepositoryFake(ProductListPage page) : IProductListRepository
    {
        public int ListCalls { get; private set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public ProductTypeId? ProductTypeId { get; private set; }

        public Task<ProductListPage> ListAsync(
            int offset,
            int limit,
            ProductTypeId? productTypeId,
            CancellationToken cancellationToken)
        {
            ListCalls++;
            Offset = offset;
            Limit = limit;
            ProductTypeId = productTypeId;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(page);
        }
    }
}
