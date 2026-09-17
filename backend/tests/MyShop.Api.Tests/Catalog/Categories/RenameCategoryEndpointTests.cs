using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class RenameCategoryEndpointTests
{
    [Fact]
    public void MapRenameCategory_MapsNamedPatchRoute()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapRenameCategory());
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/categories/{categoryId:guid}/name", endpoint.RoutePattern.RawText);
        Assert.Equal("RenameCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PATCH"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }
}
