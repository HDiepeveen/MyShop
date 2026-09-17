using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;

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
}
