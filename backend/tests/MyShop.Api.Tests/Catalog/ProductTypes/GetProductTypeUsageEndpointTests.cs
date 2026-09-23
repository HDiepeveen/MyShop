using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductTypeUsage.GetProductTypeUsage;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class GetProductTypeUsageEndpointTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    public async Task MapsUsageWithoutWriting(int products, bool inUse)
    {
        var store = new ProductTypeManagementScenario.Store();
        var entity = ProductType.Create("Entity");
        store.Items.Add(entity.Id, entity);
        store.ProductCounts[entity.Id] = products;
        var result = await GetProductTypeUsageEndpoint.ExecuteAsync(entity.Id.Value, new UseCase(store, store), CancellationToken.None);
        var value = Assert.IsType<Ok<ProductTypeUsageResponse>>(result.Result).Value!;
        Assert.Equal(entity.Id.Value, value.ProductTypeId);
        Assert.Equal(products, value.ProductCount);
        Assert.Equal(inUse, value.IsInUse);
        Assert.Equal(0, store.Saves);
        Assert.Equal(0, store.Deletes);
    }

    [Fact]
    public async Task MissingEntityIsNotFound()
    {
        var store = new ProductTypeManagementScenario.Store();
        var result = await GetProductTypeUsageEndpoint.ExecuteAsync(Guid.NewGuid(), new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Product type not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task EmptyIdIsRejectedBeforeRead()
    {
        var store = new ProductTypeManagementScenario.Store();
        var result = await GetProductTypeUsageEndpoint.ExecuteAsync(Guid.Empty, new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Invalid product type ID", Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task PreCancelledRequestDoesNotRead()
    {
        var store = new ProductTypeManagementScenario.Store();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetProductTypeUsageEndpoint.ExecuteAsync(
            Guid.NewGuid(), new UseCase(store, store), source.Token));
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task RejectsNullUseCase() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetProductTypeUsageEndpoint.ExecuteAsync(
            Guid.NewGuid(), null!, CancellationToken.None));
}
