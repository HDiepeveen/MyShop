using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MyShop.Api.Tests.Catalog;

// Executes ASP.NET's bound delegate, including JSON binding and response serialization.
// Route selection and network transport are outside this test helper.
internal sealed class CatalogManagementHttp(WebApplication app) : IAsyncDisposable
{
    public async Task<DefaultHttpContext> Send(string name, object? body = null, Guid? id = null,
        Guid? attributeId = null, string? rawBody = null, string contentType = "application/json",
        CancellationToken cancellationToken = default)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(candidate =>
                candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            RequestAborted = cancellationToken
        };
        context.Request.Method = Assert.Single(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
        if (id is not null)
        {
            context.Request.RouteValues["categoryId"] = id.Value.ToString();
            context.Request.RouteValues["productTypeId"] = id.Value.ToString();
        }
        if (attributeId is not null)
            context.Request.RouteValues["attributeId"] = attributeId.Value.ToString();
        var json = rawBody ?? (body is null ? null : JsonSerializer.Serialize(body));
        if (json is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Request.ContentType = contentType;
            context.Request.ContentLength = bytes.Length;
            context.Request.Body = new MemoryStream(bytes);
            context.Features.Set<IHttpRequestBodyDetectionFeature>(new BodyFeature(bytes.Length > 0));
        }
        context.Response.Body = new MemoryStream();
        await endpoint.RequestDelegate!(context);
        context.Response.Body.Position = 0;
        return context;
    }

    public static Task<JsonDocument> Read(DefaultHttpContext response) =>
        JsonDocument.ParseAsync(response.Response.Body);

    public ValueTask DisposeAsync() => app.DisposeAsync();

    private sealed class BodyFeature(bool canHaveBody) : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => canHaveBody;
    }
}
