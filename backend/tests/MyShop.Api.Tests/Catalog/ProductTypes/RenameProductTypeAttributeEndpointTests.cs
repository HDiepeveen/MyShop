using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductTypeAttribute.RenameProductTypeAttribute;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class RenameProductTypeAttributeEndpointTests
{
    [Fact]
    public void MapRenameProductTypeAttribute_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => RenameProductTypeAttributeEndpoint.MapRenameProductTypeAttribute(null!));

    [Fact]
    public void MapRenameProductTypeAttribute_MapsNamedPatchRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapRenameProductTypeAttribute());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}/name", endpoint.RoutePattern.RawText);
        Assert.Equal("RenameProductTypeAttribute", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PATCH"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingAttribute_ReturnsNoContent()
    {
        var store = new StoreFake(ProductType.Create("Clothing"));
        var attribute = store.ProductType!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("size"),
            "Old", AttributeDataType.Text, false, false, AttributeScope.Product);
        var result = await RenameProductTypeAttributeEndpoint.ExecuteAsync(store.ProductType.Id.Value,
            attribute.Id.Value, new("Size"), new UseCase(store, store), CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
        Assert.Equal("Size", attribute.DisplayName);
    }

    [Fact]
    public async Task ExecuteAsync_MissingProductType_ReturnsNotFound()
    {
        var store = new StoreFake(null);
        var result = await RenameProductTypeAttributeEndpoint.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(),
            new("Size"), new UseCase(store, store), CancellationToken.None);
        Assert.Equal("Product type not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => RenameProductTypeAttributeEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, null!, CancellationToken.None));

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => RenameProductTypeAttributeEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new("Size"), null!, CancellationToken.None));

    private sealed class StoreFake(ProductType? productType) : IProductTypeRepository, IProductTypeWriter
    {
        public ProductType? ProductType { get; } = productType;
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken token) => Task.FromResult(ProductType);
        public Task AddAsync(ProductType productType, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(ProductType productType, CancellationToken token) => Task.CompletedTask;
    }
}
