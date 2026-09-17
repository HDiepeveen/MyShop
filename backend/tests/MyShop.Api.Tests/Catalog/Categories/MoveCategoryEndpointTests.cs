using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.MoveCategory.MoveCategory;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class MoveCategoryEndpointTests
{
    [Fact]
    public void MapMoveCategory_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => MoveCategoryEndpoint.MapMoveCategory(null!));

    [Fact]
    public void MapMoveCategory_MapsNamedPutRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapMoveCategory());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/categories/{categoryId:guid}/parent", endpoint.RoutePattern.RawText);
        Assert.Equal("MoveCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PUT"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_ValidParent_ReturnsNoContent()
    {
        var category = Category.CreateRoot("Shirts");
        var parent = Category.CreateRoot("Clothing");
        var store = new StoreFake(category, parent);
        var result = await MoveCategoryEndpoint.ExecuteAsync(category.Id.Value,
            new(parent.Id.Value), new UseCase(store, store, store), CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(parent.Id, category.ParentCategoryId);
    }

    [Fact]
    public async Task ExecuteAsync_MissingParent_ReturnsNotFound()
    {
        var category = Category.CreateRoot("Shirts");
        var store = new StoreFake(category);
        var result = await MoveCategoryEndpoint.ExecuteAsync(category.Id.Value,
            new(Guid.NewGuid()), new UseCase(store, store, store), CancellationToken.None);
        Assert.Equal("Parent category not found",
            Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_Cycle_ReturnsConflict()
    {
        var category = Category.CreateRoot("Clothing");
        var child = Category.CreateChild("Shirts", category.Id);
        var store = new StoreFake(category, child) { IsDescendant = true };
        var result = await MoveCategoryEndpoint.ExecuteAsync(category.Id.Value,
            new(child.Id.Value), new UseCase(store, store, store), CancellationToken.None);
        Assert.Equal("Category hierarchy cycle",
            Assert.IsType<Conflict<ProblemDetails>>(result.Result).Value!.Title);
    }

    private sealed class StoreFake(params Category[] categories)
        : ICategoryRepository, ICategoryHierarchyRepository, ICategoryWriter
    {
        public bool IsDescendant { get; set; }
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token) =>
            Task.FromResult(categories.SingleOrDefault(category => category.Id == id));
        public Task<bool> IsDescendantOfAsync(CategoryId candidateId, CategoryId ancestorId,
            CancellationToken token) => Task.FromResult(IsDescendant);
        public Task AddAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(Category category, CancellationToken token) => Task.CompletedTask;
        public Task DeleteAsync(CategoryId categoryId, CancellationToken token) => throw new NotSupportedException();
    }
}
