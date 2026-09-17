using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class MoveCategoryEndpointTests
{
    [Fact]
    public void MapMoveCategory_MapsNamedPutRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapMoveCategory());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/categories/{categoryId:guid}/parent", endpoint.RoutePattern.RawText);
        Assert.Equal("MoveCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PUT"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }
}
