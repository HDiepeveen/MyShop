using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.DeleteCategory.DeleteCategory;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class DeleteCategoryEndpointTests
{
    [Fact]
    public void MapDeleteCategory_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => DeleteCategoryEndpoint.MapDeleteCategory(null!));

    [Fact]
    public void MapDeleteCategory_MapsNamedDeleteRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapDeleteCategory());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/categories/{categoryId:guid}", endpoint.RoutePattern.RawText);
        Assert.Equal("DeleteCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["DELETE"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_UnusedCategory_ReturnsNoContent()
    {
        var store = new StoreFake { Category = Category.CreateRoot("Unused") };
        var result = await DeleteCategoryEndpoint.ExecuteAsync(store.Category.Id.Value,
            new UseCase(store, store, store), CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(store.Category.Id, store.DeletedId);
    }

    [Fact]
    public async Task ExecuteAsync_MissingCategory_ReturnsNotFound()
    {
        var store = new StoreFake();
        var result = await DeleteCategoryEndpoint.ExecuteAsync(Guid.NewGuid(),
            new UseCase(store, store, store), CancellationToken.None);
        Assert.Equal("Category not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_UsedCategory_ReturnsConflictWithCounts()
    {
        var store = new StoreFake
        {
            Category = Category.CreateRoot("Used"),
            Usage = new CategoryUsage(2, 3)
        };
        var result = await DeleteCategoryEndpoint.ExecuteAsync(store.Category.Id.Value,
            new UseCase(store, store, store), CancellationToken.None);
        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("Category is in use", conflict.Value!.Title);
        Assert.Contains("2 direct children", conflict.Value.Detail);
        Assert.Contains("3 product assignments", conflict.Value.Detail);
    }

    private sealed class StoreFake : ICategoryRepository, ICategoryUsageRepository, ICategoryWriter
    {
        public Category? Category { get; set; }
        public CategoryUsage Usage { get; set; } = new(0, 0);
        public CategoryId? DeletedId { get; private set; }
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token) => Task.FromResult(Category);
        public Task<CategoryUsage> GetUsageAsync(CategoryId id, CancellationToken token) => Task.FromResult(Usage);
        public Task AddAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteAsync(CategoryId categoryId, CancellationToken token)
        {
            DeletedId = categoryId;
            return Task.CompletedTask;
        }
    }
}
