using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateProductType.CreateProductType;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class CreateProductTypeEndpointTests
{
    [Fact]
    public void MapCreateProductType_MapsNamedPostRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapCreateProductType());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app)
            .DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types", endpoint.RoutePattern.RawText);
        Assert.Equal("CreateProductType", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["POST"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCreatedProductType()
    {
        var writer = new WriterFake();
        var result = await CreateProductTypeEndpoint.ExecuteAsync(
            new CreateProductTypeRequest("Clothing"), new UseCase(writer), CancellationToken.None);

        var created = Assert.IsType<Created<CreateProductTypeResponse>>(result.Result);
        Assert.Equal($"/api/product-types/{writer.Added!.Id.Value}", created.Location);
        Assert.Equal(writer.Added.Id.Value, created.Value!.Id);
        Assert.Equal("Clothing", created.Value.Name);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidName_ReturnsBadRequest()
    {
        var result = await CreateProductTypeEndpoint.ExecuteAsync(
            new CreateProductTypeRequest(" "), new UseCase(new WriterFake()), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product type", badRequest.Value!.Title);
    }

    private sealed class WriterFake : IProductTypeWriter
    {
        public ProductType? Added { get; private set; }
        public Task AddAsync(ProductType productType, CancellationToken cancellationToken)
        {
            Added = productType;
            return Task.CompletedTask;
        }
        public Task SaveAsync(ProductType productType, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
