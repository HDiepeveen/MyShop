using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Catalog.Categories;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetCategoryUsage.GetCategoryUsage;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class GetCategoryUsageEndpointTests
{
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, true)]
    [InlineData(0, 3, true)]
    [InlineData(2, 3, true)]
    public async Task MapsUsageWithoutWriting(int children, int assignments, bool inUse)
    {
        var store = new CategoryManagementScenario.Store();
        var entity = Category.CreateRoot("Entity");
        store.Items.Add(entity.Id, entity);
        for (var i = 0; i < children; i++)
        {
            var child = Category.CreateChild("Child", entity.Id);
            store.Items.Add(child.Id, child);
        }
        store.Assignments[entity.Id] = assignments;
        var result = await GetCategoryUsageEndpoint.ExecuteAsync(entity.Id.Value, new UseCase(store, store), CancellationToken.None);
        var value = Assert.IsType<Ok<CategoryUsageResponse>>(result.Result).Value!;
        Assert.Equal(entity.Id.Value, value.CategoryId);
        Assert.Equal(children, value.DirectChildCount);
        Assert.Equal(assignments, value.ProductAssignmentCount);
        Assert.Equal(inUse, value.IsInUse);
        Assert.Equal(0, store.Saves);
        Assert.Equal(0, store.Deletes);
    }

    [Fact]
    public async Task MissingEntityIsNotFound()
    {
        var store = new CategoryManagementScenario.Store();
        var result = await GetCategoryUsageEndpoint.ExecuteAsync(Guid.NewGuid(), new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Category not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task EmptyIdIsRejectedBeforeRead()
    {
        var store = new CategoryManagementScenario.Store();
        var result = await GetCategoryUsageEndpoint.ExecuteAsync(Guid.Empty, new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Invalid category ID", Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task PreCancelledRequestDoesNotRead()
    {
        var store = new CategoryManagementScenario.Store();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetCategoryUsageEndpoint.ExecuteAsync(
            Guid.NewGuid(), new UseCase(store, store), source.Token));
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task RejectsNullUseCase() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetCategoryUsageEndpoint.ExecuteAsync(
            Guid.NewGuid(), null!, CancellationToken.None));
}
