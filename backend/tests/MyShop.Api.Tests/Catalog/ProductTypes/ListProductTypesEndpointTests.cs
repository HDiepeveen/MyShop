using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using UseCase = MyShop.Application.Catalog.ListProductTypes.ListProductTypes;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ListProductTypesEndpointTests
{
    [Fact]
    public void MapListProductTypes_MapsNamedGetRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapListProductTypes());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types", endpoint.RoutePattern.RawText);
        Assert.Equal("ListProductTypes", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_MapsRepositoryResults()
    {
        var first = new ProductTypeListItem(Guid.NewGuid(), "Clothing");
        var second = new ProductTypeListItem(Guid.NewGuid(), "Shoes");
        var repository = new ProductTypeListRepositoryFake([first, second]);
        var useCase = new UseCase(repository);

        var result = await ListProductTypesEndpoint.ExecuteAsync(null, useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<ProductTypeSummaryResponse>>>(result.Result);
        Assert.Collection(Assert.IsAssignableFrom<IReadOnlyList<ProductTypeSummaryResponse>>(ok.Value),
            item =>
            {
                Assert.Equal(first.Id, item.Id);
                Assert.Equal(first.Name, item.Name);
            },
            item =>
            {
                Assert.Equal(second.Id, item.Id);
                Assert.Equal(second.Name, item.Name);
            });
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoProductTypesExist_ReturnsEmptyList()
    {
        var useCase = new UseCase(new ProductTypeListRepositoryFake([]));

        var result = await ListProductTypesEndpoint.ExecuteAsync(null, useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<ProductTypeSummaryResponse>>>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<ProductTypeSummaryResponse>>(ok.Value));
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var repository = new ProductTypeListRepositoryFake([]);
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ListProductTypesEndpoint.ExecuteAsync(null, useCase, source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_TrimsAndForwardsSearch()
    {
        var repository = new ProductTypeListRepositoryFake([]);
        var useCase = new UseCase(repository);

        var result = await ListProductTypesEndpoint.ExecuteAsync(
            "  cloth  ", useCase, CancellationToken.None);

        Assert.IsType<Ok<IReadOnlyList<ProductTypeSummaryResponse>>>(result.Result);
        Assert.Equal("cloth", repository.SearchTerm);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExecuteAsync_WhenSearchIsEmpty_ReturnsBadRequest(string search)
    {
        var repository = new ProductTypeListRepositoryFake([]);
        var useCase = new UseCase(repository);

        var result = await ListProductTypesEndpoint.ExecuteAsync(
            search, useCase, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product type search", badRequest.Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    private sealed class ProductTypeListRepositoryFake(IReadOnlyList<ProductTypeListItem> productTypes)
        : IProductTypeListRepository
    {
        public int ListCalls { get; private set; }
        public string? SearchTerm { get; private set; }

        public Task<IReadOnlyList<ProductTypeListItem>> ListAsync(
            string? searchTerm,
            CancellationToken cancellationToken)
        {
            ListCalls++;
            SearchTerm = searchTerm;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(productTypes);
        }
    }
}
