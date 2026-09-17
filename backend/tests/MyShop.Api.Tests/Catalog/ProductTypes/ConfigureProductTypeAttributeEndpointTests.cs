using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ConfigureProductTypeAttributeEndpointTests
{
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
}
