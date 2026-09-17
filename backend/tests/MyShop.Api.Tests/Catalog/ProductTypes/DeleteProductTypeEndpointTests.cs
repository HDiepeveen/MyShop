using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.DeleteProductType.DeleteProductType;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class DeleteProductTypeEndpointTests
{
    [Fact]
    public void MapDeleteProductType_MapsNamedDeleteRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapDeleteProductType());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types/{productTypeId:guid}", endpoint.RoutePattern.RawText);
        Assert.Equal("DeleteProductType", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["DELETE"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_UnusedProductType_ReturnsNoContent()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Unused") };
        var result = await DeleteProductTypeEndpoint.ExecuteAsync(store.ProductType.Id.Value,
            new UseCase(store, store, store), CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(store.ProductType.Id, store.DeletedId);
    }

    [Fact]
    public async Task ExecuteAsync_UsedProductType_ReturnsConflictWithCount()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Used"), ProductCount = 4 };
        var result = await DeleteProductTypeEndpoint.ExecuteAsync(store.ProductType.Id.Value,
            new UseCase(store, store, store), CancellationToken.None);
        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("Product type is in use", conflict.Value!.Title);
        Assert.Contains("4 products", conflict.Value.Detail);
    }

    private sealed class StoreFake : IProductTypeRepository, IProductTypeUsageRepository, IProductTypeDeleter
    {
        public ProductType? ProductType { get; set; }
        public int ProductCount { get; set; }
        public ProductTypeId? DeletedId { get; private set; }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken token) => Task.FromResult(ProductType);
        public Task<int> CountProductsAsync(ProductTypeId id, CancellationToken token) => Task.FromResult(ProductCount);
        public Task DeleteAsync(ProductTypeId id, CancellationToken token)
        {
            DeletedId = id;
            return Task.CompletedTask;
        }
    }
}
