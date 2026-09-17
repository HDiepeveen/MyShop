using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductType.RenameProductType;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class RenameProductTypeEndpointTests
{
    [Fact]
    public void MapRenameProductType_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => RenameProductTypeEndpoint.MapRenameProductType(null!));

    [Fact]
    public void MapRenameProductType_MapsNamedPatchRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapRenameProductType());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types/{productTypeId:guid}/name", endpoint.RoutePattern.RawText);
        Assert.Equal("RenameProductType", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PATCH"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingProductType_ReturnsNoContent()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Old") };
        var result = await RenameProductTypeEndpoint.ExecuteAsync(
            store.ProductType.Id.Value, new RenameProductTypeRequest("New"),
            new UseCase(store, store), CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async Task ExecuteAsync_MissingProductType_ReturnsNotFound()
    {
        var store = new StoreFake();
        var result = await RenameProductTypeEndpoint.ExecuteAsync(
            Guid.NewGuid(), new RenameProductTypeRequest("New"),
            new UseCase(store, store), CancellationToken.None);
        Assert.IsType<NotFound<ProblemDetails>>(result.Result);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidRequest_ReturnsBadRequest()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Old") };
        var result = await RenameProductTypeEndpoint.ExecuteAsync(
            store.ProductType.Id.Value, new RenameProductTypeRequest(" "),
            new UseCase(store, store), CancellationToken.None);
        Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => RenameProductTypeEndpoint.ExecuteAsync(
            Guid.NewGuid(), null!, null!, CancellationToken.None));

    private sealed class StoreFake : IProductTypeRepository, IProductTypeWriter
    {
        public ProductType? ProductType { get; set; }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken) =>
            Task.FromResult(ProductType);
        public Task AddAsync(ProductType productType, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task SaveAsync(ProductType productType, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
