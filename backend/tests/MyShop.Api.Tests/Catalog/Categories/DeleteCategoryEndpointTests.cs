using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class DeleteCategoryEndpointTests
{
    [Fact]
    public void MapDeleteCategory_MapsNamedDeleteRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapDeleteCategory());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/categories/{categoryId:guid}", endpoint.RoutePattern.RawText);
        Assert.Equal("DeleteCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["DELETE"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }
}
