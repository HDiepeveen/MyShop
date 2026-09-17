using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameCategory.RenameCategory;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class RenameCategoryEndpointTests
{
    [Fact]
    public void MapRenameCategory_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => RenameCategoryEndpoint.MapRenameCategory(null!));

    [Fact]
    public void MapRenameCategory_MapsNamedPatchRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapRenameCategory());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/categories/{categoryId:guid}/name", endpoint.RoutePattern.RawText);
        Assert.Equal("RenameCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PATCH"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingCategory_ReturnsNoContent()
    {
        var store = new StoreFake { Category = Category.CreateRoot("Old") };
        var result = await RenameCategoryEndpoint.ExecuteAsync(store.Category.Id.Value,
            new("New"), new UseCase(store, store), CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
        Assert.Equal("New", store.Category.Name);
    }

    [Fact]
    public async Task ExecuteAsync_MissingCategory_ReturnsNotFound()
    {
        var store = new StoreFake();
        var result = await RenameCategoryEndpoint.ExecuteAsync(Guid.NewGuid(),
            new("New"), new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Category not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidName_ReturnsBadRequest()
    {
        var store = new StoreFake { Category = Category.CreateRoot("Old") };
        var result = await RenameCategoryEndpoint.ExecuteAsync(store.Category.Id.Value,
            new(" "), new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Invalid category rename", Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
    }

    private sealed class StoreFake : ICategoryRepository, ICategoryWriter
    {
        public Category? Category { get; set; }
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token) => Task.FromResult(Category);
        public Task AddAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(Category category, CancellationToken token) => Task.CompletedTask;
        public Task DeleteAsync(CategoryId categoryId, CancellationToken token) => throw new NotSupportedException();
    }
}
