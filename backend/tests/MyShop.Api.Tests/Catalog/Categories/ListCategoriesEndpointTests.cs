using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
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

        var result = await ListCategoriesEndpoint.ExecuteAsync(useCase, CancellationToken.None);

        Assert.Collection(Assert.IsAssignableFrom<IReadOnlyList<CategorySummaryResponse>>(result.Value),
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

        var result = await ListCategoriesEndpoint.ExecuteAsync(useCase, CancellationToken.None);

        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<CategorySummaryResponse>>(result.Value));
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var repository = new CategoryListRepositoryFake([]);
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ListCategoriesEndpoint.ExecuteAsync(useCase, source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    private sealed class CategoryListRepositoryFake(IReadOnlyList<CategoryListItem> categories)
        : ICategoryListRepository
    {
        public int ListCalls { get; private set; }

        public Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken cancellationToken)
        {
            ListCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(categories);
        }
    }
}
