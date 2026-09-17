using Microsoft.AspNetCore.Http.HttpResults;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateCategory.CreateCategory;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class CreateCategoryEndpointTests
{
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

    private sealed class StoreFake : ICategoryRepository, ICategoryWriter
    {
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken token) => Task.FromResult<Category?>(null);
        public Task AddAsync(Category category, CancellationToken token) => Task.CompletedTask;
        public Task SaveAsync(Category category, CancellationToken token) => throw new NotSupportedException();
    }
}
