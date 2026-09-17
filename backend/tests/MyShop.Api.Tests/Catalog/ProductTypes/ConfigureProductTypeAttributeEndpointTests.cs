using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ConfigureProductTypeAttribute.ConfigureProductTypeAttribute;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ConfigureProductTypeAttributeEndpointTests
{
    [Fact]
    public void MapConfigureProductTypeAttribute_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ConfigureProductTypeAttributeEndpoint.MapConfigureProductTypeAttribute(null!));

    [Fact]
    public void MapConfigureProductTypeAttribute_MapsNamedPutRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapConfigureProductTypeAttribute());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}/configuration", endpoint.RoutePattern.RawText);
        Assert.Equal("ConfigureProductTypeAttribute", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PUT"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingAttribute_ReturnsNoContent()
    {
        var store = new StoreFake(ProductType.Create("Clothing"));
        var attribute = store.ProductType!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("size"),
            "Size", AttributeDataType.Text, false, false, AttributeScope.Product);
        var result = await ConfigureProductTypeAttributeEndpoint.ExecuteAsync(store.ProductType.Id.Value,
            attribute.Id.Value, new(true, true), new UseCase(store, store), CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
        Assert.True(attribute.IsRequired);
        Assert.True(attribute.IsFilterable);
    }

    [Fact]
    public async Task ExecuteAsync_MissingAttribute_ReturnsNotFound()
    {
        var store = new StoreFake(ProductType.Create("Clothing"));
        var result = await ConfigureProductTypeAttributeEndpoint.ExecuteAsync(store.ProductType!.Id.Value,
            Guid.NewGuid(), new(true, true), new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Attribute not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => ConfigureProductTypeAttributeEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, null!, CancellationToken.None));

    private sealed class StoreFake(ProductType? productType) : IProductTypeRepository, IProductTypeWriter
    {
        public ProductType? ProductType { get; } = productType;
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken token) => Task.FromResult(ProductType);
        public Task AddAsync(ProductType productType, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(ProductType productType, CancellationToken token) => Task.CompletedTask;
    }
}
