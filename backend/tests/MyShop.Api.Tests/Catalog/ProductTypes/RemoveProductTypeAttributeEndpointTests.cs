using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductTypeAttribute.RemoveProductTypeAttribute;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class RemoveProductTypeAttributeEndpointTests
{
    [Fact]
    public void MapRemoveProductTypeAttribute_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => RemoveProductTypeAttributeEndpoint.MapRemoveProductTypeAttribute(null!));

    [Fact]
    public void MapRemoveProductTypeAttribute_MapsNamedDeleteRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapRemoveProductTypeAttribute());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}", endpoint.RoutePattern.RawText);
        Assert.Equal("RemoveProductTypeAttribute", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["DELETE"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingAttribute_ReturnsNoContent()
    {
        var store = new StoreFake(ProductType.Create("Clothing"));
        var attribute = store.ProductType!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("size"),
            "Size", AttributeDataType.Text, false, false, AttributeScope.Product);
        var result = await RemoveProductTypeAttributeEndpoint.ExecuteAsync(store.ProductType.Id.Value,
            attribute.Id.Value, new UseCase(store, store), CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
        Assert.Empty(store.ProductType.AttributeDefinitions);
    }

    [Fact]
    public async Task ExecuteAsync_MissingAttribute_ReturnsNotFound()
    {
        var store = new StoreFake(ProductType.Create("Clothing"));
        var result = await RemoveProductTypeAttributeEndpoint.ExecuteAsync(store.ProductType!.Id.Value,
            Guid.NewGuid(), new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Attribute not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => RemoveProductTypeAttributeEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));

    private sealed class StoreFake(ProductType? productType) : IProductTypeRepository, IProductTypeWriter
    {
        public ProductType? ProductType { get; } = productType;
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken token) => Task.FromResult(ProductType);
        public Task AddAsync(ProductType productType, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(ProductType productType, CancellationToken token) => Task.CompletedTask;
    }
}
