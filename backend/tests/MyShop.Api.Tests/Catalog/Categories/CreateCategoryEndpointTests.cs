using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateCategory.CreateCategory;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class CreateCategoryEndpointTests
{
    [Fact]
    public void MapCreateCategory_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CreateCategoryEndpoint.MapCreateCategory(null!));

    [Fact]
    public void MapCreateCategory_MapsNamedPostRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapCreateCategory());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/categories", endpoint.RoutePattern.RawText);
        Assert.Equal("CreateCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["POST"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesRootCategory()
    {
        var store = new StoreFake();
        var result = await CreateCategoryEndpoint.ExecuteAsync(
            new("Clothing"), new UseCase(store, store), CancellationToken.None);
        var created = Assert.IsType<Created<CreateCategoryResponse>>(result.Result);
        Assert.Equal("Clothing", created.Value!.Name);
        Assert.Null(created.Value.ParentCategoryId);
    }

    [Fact]
    public async Task ExecuteAsync_MissingParent_ReturnsNotFound()
    {
        var store = new StoreFake();
        var result = await CreateCategoryEndpoint.ExecuteAsync(
            new("Shirts", Guid.NewGuid()), new UseCase(store, store), CancellationToken.None);
        Assert.IsType<NotFound<Microsoft.AspNetCore.Mvc.ProblemDetails>>(result.Result);
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateCategoryEndpoint.ExecuteAsync(
            null!, null!, CancellationToken.None));

    private sealed class StoreFake : ICategoryRepository, ICategoryWriter
    {
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token) => Task.FromResult<Category?>(null);
        public Task AddAsync(Category category, CancellationToken token) => Task.CompletedTask;
        public Task SaveAsync(Category category, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteAsync(CategoryId categoryId, CancellationToken token) => throw new NotSupportedException();
    }
}
