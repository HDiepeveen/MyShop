using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListCategories.ListCategories;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class ListCategoriesEndpointTests
{
    [Fact]
    public void MapListCategories_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ListCategoriesEndpoint.MapListCategories(null!));

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
        var root = new CategoryListItem(parentId, "Clothing", null, 1);
        var child = new CategoryListItem(Guid.NewGuid(), "Shirts", parentId, 0);
        var useCase = new UseCase(new CategoryListRepositoryFake([root, child]));

        var result = await ListCategoriesEndpoint.ExecuteAsync(
            null, null, null, null, null, useCase, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<CategorySummaryResponse>>>(result.Result);
        Assert.Collection(Assert.IsAssignableFrom<IReadOnlyList<CategorySummaryResponse>>(ok.Value),
            item =>
            {
                Assert.Equal(root.Id, item.Id);
                Assert.Equal(root.Name, item.Name);
                Assert.Null(item.ParentCategoryId);
                Assert.True(item.IsRoot);
                Assert.Equal(root.DirectChildCount, item.DirectChildCount);
            },
            item =>
            {
                Assert.Equal(child.Id, item.Id);
                Assert.Equal(child.Name, item.Name);
                Assert.Equal(parentId, item.ParentCategoryId);
                Assert.False(item.IsRoot);
                Assert.Equal(child.DirectChildCount, item.DirectChildCount);
            });
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoCategoriesExist_ReturnsEmptyList()
    {
        var useCase = new UseCase(new CategoryListRepositoryFake([]));

        var result = await ListCategoriesEndpoint.ExecuteAsync(
            null, null, null, null, null, useCase, CancellationToken.None);

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
            ListCategoriesEndpoint.ExecuteAsync(
                null, null, null, null, null, useCase, source.Token));

        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_TrimsAndForwardsSearch()
    {
        var repository = new CategoryListRepositoryFake([]);
        var useCase = new UseCase(repository);

        var result = await ListCategoriesEndpoint.ExecuteAsync(
            null, null, "  shirt  ", null, null, useCase, CancellationToken.None);

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
            null, null, search, null, null, useCase, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid category filter", badRequest.Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_FiltersByParentCategory()
    {
        var repository = new CategoryListRepositoryFake([]);
        var useCase = new UseCase(repository);
        var parentId = Guid.NewGuid();

        var result = await ListCategoriesEndpoint.ExecuteAsync(
            null, null, null, parentId, null, useCase, CancellationToken.None);

        Assert.IsType<Ok<IReadOnlyList<CategorySummaryResponse>>>(result.Result);
        Assert.Equal(CategoryId.From(parentId), repository.ParentCategoryId);
        Assert.False(repository.RootsOnly);
    }

    [Fact]
    public async Task ExecuteAsync_FiltersRoots()
    {
        var repository = new CategoryListRepositoryFake([]);
        var useCase = new UseCase(repository);

        var result = await ListCategoriesEndpoint.ExecuteAsync(
            null, null, null, null, true, useCase, CancellationToken.None);

        Assert.IsType<Ok<IReadOnlyList<CategorySummaryResponse>>>(result.Result);
        Assert.True(repository.RootsOnly);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsCombinedRootAndParentFilters()
    {
        var repository = new CategoryListRepositoryFake([]);
        var useCase = new UseCase(repository);

        var result = await ListCategoriesEndpoint.ExecuteAsync(
            null, null, null, Guid.NewGuid(), true, useCase, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid category filter", badRequest.Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => ListCategoriesEndpoint.ExecuteAsync(
            null, null, null, null, null, null!, CancellationToken.None));

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 101)]
    public async Task ExecuteAsync_InvalidPageReturnsBadRequestBeforeRead(int offset, int limit)
    {
        var repository = new CategoryListRepositoryFake([]);
        var result = await ListCategoriesEndpoint.ExecuteAsync(
            offset, limit, null, null, null, new UseCase(repository), CancellationToken.None);
        Assert.Equal("Invalid category paging", Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
        Assert.Equal(0, repository.ListCalls);
    }

    [Theory]
    [InlineData(null, null, 0, 50)]
    [InlineData(0, 1, 0, 1)]
    [InlineData(2147483647, 100, 2147483647, 100)]
    public async Task ExecuteAsync_ForwardsEffectivePage(int? offset, int? limit, int expectedOffset, int expectedLimit)
    {
        var repository = new CategoryListRepositoryFake([]);
        var result = await ListCategoriesEndpoint.ExecuteAsync(
            offset, limit, null, null, null, new UseCase(repository), CancellationToken.None);
        Assert.IsType<Ok<IReadOnlyList<CategorySummaryResponse>>>(result.Result);
        Assert.Equal(expectedOffset, repository.Offset);
        Assert.Equal(expectedLimit, repository.Limit);
    }

    private sealed class CategoryListRepositoryFake(IReadOnlyList<CategoryListItem> categories)
        : ICategoryListRepository
    {
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public int ListCalls { get; private set; }
        public string? SearchTerm { get; private set; }
        public CategoryId? ParentCategoryId { get; private set; }
        public bool RootsOnly { get; private set; }

        public Task<IReadOnlyList<CategoryListItem>> ListAsync(
            int offset,
            int limit,
            string? searchTerm,
            CategoryId? parentCategoryId,
            bool rootsOnly,
            CancellationToken cancellationToken)
        {
            Offset = offset;
            Limit = limit;
            ListCalls++;
            SearchTerm = searchTerm;
            ParentCategoryId = parentCategoryId;
            RootsOnly = rootsOnly;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(categories);
        }
    }
}
