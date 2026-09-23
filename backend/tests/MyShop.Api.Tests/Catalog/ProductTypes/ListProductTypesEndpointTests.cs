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
    public void MapListProductTypes_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ListProductTypesEndpoint.MapListProductTypes(null!));

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
        var first = new ProductTypeListItem(Guid.NewGuid(), "Clothing", 3);
        var second = new ProductTypeListItem(Guid.NewGuid(), "Shoes", 2);
        var repository = new ProductTypeListRepositoryFake([first, second]);
        var useCase = new UseCase(repository);

        var result = await ListProductTypesEndpoint.ExecuteAsync(
            null, null, null, useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<ProductTypeSummaryResponse>>>(result.Result);
        Assert.Collection(Assert.IsAssignableFrom<IReadOnlyList<ProductTypeSummaryResponse>>(ok.Value),
            item =>
            {
                Assert.Equal(first.Id, item.Id);
                Assert.Equal(first.Name, item.Name);
                Assert.Equal(first.AttributeDefinitionCount, item.AttributeDefinitionCount);
            },
            item =>
            {
                Assert.Equal(second.Id, item.Id);
                Assert.Equal(second.Name, item.Name);
                Assert.Equal(second.AttributeDefinitionCount, item.AttributeDefinitionCount);
            });
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoProductTypesExist_ReturnsEmptyList()
    {
        var useCase = new UseCase(new ProductTypeListRepositoryFake([]));

        var result = await ListProductTypesEndpoint.ExecuteAsync(
            null, null, null, useCase, CancellationToken.None);

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
            ListProductTypesEndpoint.ExecuteAsync(
                null, null, null, useCase, source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_TrimsAndForwardsSearch()
    {
        var repository = new ProductTypeListRepositoryFake([]);
        var useCase = new UseCase(repository);

        var result = await ListProductTypesEndpoint.ExecuteAsync(
            null, null, "  cloth  ", useCase, CancellationToken.None);

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
            null, null, search, useCase, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product type search", badRequest.Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => ListProductTypesEndpoint.ExecuteAsync(
            null, null, null, null!, CancellationToken.None));

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 101)]
    public async Task ExecuteAsync_InvalidPageReturnsBadRequestBeforeRead(int offset, int limit)
    {
        var repository = new ProductTypeListRepositoryFake([]);
        var result = await ListProductTypesEndpoint.ExecuteAsync(
            offset, limit, null, new UseCase(repository), CancellationToken.None);
        Assert.Equal("Invalid product type paging", Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    [Theory]
    [InlineData(null, null, 0, 50)]
    [InlineData(0, 1, 0, 1)]
    [InlineData(2147483647, 100, 2147483647, 100)]
    public async Task ExecuteAsync_ForwardsEffectivePage(int? offset, int? limit, int expectedOffset, int expectedLimit)
    {
        var repository = new ProductTypeListRepositoryFake([]);
        var result = await ListProductTypesEndpoint.ExecuteAsync(
            offset, limit, null, new UseCase(repository), CancellationToken.None);
        Assert.IsType<Ok<IReadOnlyList<ProductTypeSummaryResponse>>>(result.Result);
        Assert.Equal(expectedOffset, repository.Offset);
        Assert.Equal(expectedLimit, repository.Limit);
    }

    private sealed class ProductTypeListRepositoryFake(IReadOnlyList<ProductTypeListItem> productTypes)
        : IProductTypeListRepository
    {
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public int ListCalls { get; private set; }
        public string? SearchTerm { get; private set; }

        public Task<IReadOnlyList<ProductTypeListItem>> ListAsync(
            int offset,
            int limit,
            string? searchTerm,
            CancellationToken cancellationToken)
        {
            Offset = offset;
            Limit = limit;
            ListCalls++;
            SearchTerm = searchTerm;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(productTypes);
        }
    }
}
