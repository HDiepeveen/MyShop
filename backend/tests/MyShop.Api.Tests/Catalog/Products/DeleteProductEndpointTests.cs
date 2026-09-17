using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.DeleteProduct.DeleteProduct;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class DeleteProductEndpointTests
{
    [Fact]
    public void MapDeleteProduct_MapsNamedDeleteRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapDeleteProduct());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/products/{productId:guid}", endpoint.RoutePattern.RawText);
        Assert.Equal("DeleteProduct", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["DELETE"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public void MapDeleteProduct_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => DeleteProductEndpoint.MapDeleteProduct(null!));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ReturnsExpectedOutcome(bool deleted)
    {
        var result = await DeleteProductEndpoint.ExecuteAsync(Guid.NewGuid(),
            new UseCase(new DeleterFake(deleted)), CancellationToken.None);
        if (deleted) Assert.IsType<NoContent>(result.Result);
        else Assert.Equal("Product not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DeleteProductEndpoint.ExecuteAsync(Guid.NewGuid(), null!, CancellationToken.None));

    private sealed class DeleterFake(bool outcome) : IProductDeleter
    {
        public Task<bool> DeleteAsync(ProductId productId, CancellationToken cancellationToken) =>
            Task.FromResult(outcome);
    }
}
