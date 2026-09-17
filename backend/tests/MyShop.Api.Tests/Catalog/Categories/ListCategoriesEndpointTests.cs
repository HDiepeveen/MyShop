using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using UseCase = MyShop.Application.Catalog.ListCategories.ListCategories;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class ListCategoriesEndpointTests
{
    [Fact]
    public void MapListCategories_MapsNamedGetRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapListCategories());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal("/api/categories", endpoint.RoutePattern.RawText);
        Assert.Equal("ListCategories", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_MapsRootAndChildCategories()
    {
        var parentId = Guid.NewGuid();
        var root = new CategoryListItem(parentId, "Clothing", null);
        var child = new CategoryListItem(Guid.NewGuid(), "Shirts", parentId);
        var useCase = new UseCase(new CategoryListRepositoryFake([root, child]));

        var result = await ListCategoriesEndpoint.ExecuteAsync(null, useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<CategorySummaryResponse>>>(result.Result);
        Assert.Collection(Assert.IsAssignableFrom<IReadOnlyList<CategorySummaryResponse>>(ok.Value),
            item =>
            {
                Assert.Equal(root.Id, item.Id);
                Assert.Equal(root.Name, item.Name);
                Assert.Null(item.ParentCategoryId);
                Assert.True(item.IsRoot);
            },
            item =>
            {
                Assert.Equal(child.Id, item.Id);
                Assert.Equal(child.Name, item.Name);
                Assert.Equal(parentId, item.ParentCategoryId);
                Assert.False(item.IsRoot);
            });
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoCategoriesExist_ReturnsEmptyList()
    {
        var useCase = new UseCase(new CategoryListRepositoryFake([]));

        var result = await ListCategoriesEndpoint.ExecuteAsync(null, useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<CategorySummaryResponse>>>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<CategorySummaryResponse>>(ok.Value));
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var repository = new CategoryListRepositoryFake([]);
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ListCategoriesEndpoint.ExecuteAsync(null, useCase, source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_TrimsAndForwardsSearch()
    {
        var repository = new CategoryListRepositoryFake([]);
        var useCase = new UseCase(repository);

        var result = await ListCategoriesEndpoint.ExecuteAsync(
            "  shirt  ", useCase, CancellationToken.None);

        Assert.IsType<Ok<IReadOnlyList<CategorySummaryResponse>>>(result.Result);
        Assert.Equal("shirt", repository.SearchTerm);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExecuteAsync_WhenSearchIsEmpty_ReturnsBadRequest(string search)
    {
        var repository = new CategoryListRepositoryFake([]);
        var useCase = new UseCase(repository);

        var result = await ListCategoriesEndpoint.ExecuteAsync(
            search, useCase, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid category search", badRequest.Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    private sealed class CategoryListRepositoryFake(IReadOnlyList<CategoryListItem> categories)
        : ICategoryListRepository
    {
        public int ListCalls { get; private set; }
        public string? SearchTerm { get; private set; }

        public Task<IReadOnlyList<CategoryListItem>> ListAsync(
            string? searchTerm,
            CancellationToken cancellationToken)
        {
            ListCalls++;
            SearchTerm = searchTerm;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(categories);
        }
    }
}
