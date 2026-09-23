using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MyShop.Api.Tests.Catalog;

internal static class CatalogListHttp
{
    internal static async Task<DefaultHttpContext> Execute(
        WebApplication app, string query, CancellationToken cancellationToken = default)
    {
        var endpoint = Assert.IsType<RouteEndpoint>(
            ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Single());
        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            RequestAborted = cancellationToken
        };
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString(query);
        context.Response.Body = new MemoryStream();
        await endpoint.RequestDelegate!(context);
        context.Response.Body.Position = 0;
        return context;
    }
}
