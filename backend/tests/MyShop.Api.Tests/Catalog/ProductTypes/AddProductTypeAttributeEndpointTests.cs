using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AddProductTypeAttribute.AddProductTypeAttribute;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class AddProductTypeAttributeEndpointTests
{
    [Fact]
    public void MapAddProductTypeAttribute_MapsNamedPostRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapAddProductTypeAttribute());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types/{productTypeId:guid}/attributes", endpoint.RoutePattern.RawText);
        Assert.Equal("AddProductTypeAttribute", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["POST"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_AddsAttributeAndReturnsCreated()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Clothing") };
        var request = new AddProductTypeAttributeRequest(
            "colour", "Colour", AttributeDataType.Choice, false, true, AttributeScope.Variant);
        var result = await AddProductTypeAttributeEndpoint.ExecuteAsync(
            store.ProductType.Id.Value, request, new UseCase(store, store), CancellationToken.None);

        var created = Assert.IsType<Created<AddProductTypeAttributeResponse>>(result.Result);
        Assert.Equal("colour", created.Value!.Code);
        Assert.Contains(created.Value.Id.ToString(), created.Location);
    }

    [Fact]
    public async Task ExecuteAsync_MissingProductType_ReturnsNotFound()
    {
        var store = new StoreFake();
        var result = await AddProductTypeAttributeEndpoint.ExecuteAsync(
            Guid.NewGuid(), new("code", "Name", AttributeDataType.Text, false, false, AttributeScope.Product),
            new UseCase(store, store), CancellationToken.None);
        Assert.IsType<NotFound<Microsoft.AspNetCore.Mvc.ProblemDetails>>(result.Result);
    }

    private sealed class StoreFake : IProductTypeRepository, IProductTypeWriter
    {
        public ProductType? ProductType { get; set; }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken token) => Task.FromResult(ProductType);
        public Task AddAsync(ProductType productType, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(ProductType productType, CancellationToken token) => Task.CompletedTask;
    }
}
