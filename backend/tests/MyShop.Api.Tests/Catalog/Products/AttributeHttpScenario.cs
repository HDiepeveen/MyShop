using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using SetProduct = MyShop.Application.Catalog.SetProductAttributeValue.SetProductAttributeValue;
using SetVariant = MyShop.Application.Catalog.SetVariantAttributeValue.SetVariantAttributeValue;
using GetProduct = MyShop.Application.Catalog.GetProductAttributeValue.GetProductAttributeValue;
using GetVariant = MyShop.Application.Catalog.GetVariantAttributeValue.GetVariantAttributeValue;
using RemoveProduct = MyShop.Application.Catalog.RemoveProductAttributeValue.RemoveProductAttributeValue;
using RemoveVariant = MyShop.Application.Catalog.RemoveVariantAttributeValue.RemoveVariantAttributeValue;

namespace MyShop.Api.Tests.Catalog.Products;

internal sealed class AttributeHttpScenario : IAsyncDisposable
{
    private readonly WebApplication _app;

    public AttributeHttpScenario(AttributeDataType dataType, AttributeScope scope)
    {
        Repository = new RepositoryFake(dataType, scope);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IProductRepository>(Repository);
        builder.Services.AddSingleton<IProductTypeRepository>(Repository);
        builder.Services.AddScoped<SetProduct>();
        builder.Services.AddScoped<SetVariant>();
        builder.Services.AddScoped<GetProduct>();
        builder.Services.AddScoped<GetVariant>();
        builder.Services.AddScoped<RemoveProduct>();
        builder.Services.AddScoped<RemoveVariant>();
        _app = builder.Build();
        _app.MapSetProductAttributeValue();
        _app.MapSetVariantAttributeValue();
        _app.MapGetProductAttributeValue();
        _app.MapGetVariantAttributeValue();
        _app.MapRemoveProductAttributeValue();
        _app.MapRemoveVariantAttributeValue();
    }

    public RepositoryFake Repository { get; }

    public async Task<DefaultHttpContext> Send(string name, string? body = null, Guid? variantId = null)
    {
        var endpoint = ((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
        await using var scope = _app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = Assert.Single(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
        context.Request.RouteValues["productId"] = Repository.Product.Id.Value.ToString();
        context.Request.RouteValues["variantId"] = (variantId ?? Repository.Variant.Id.Value).ToString();
        context.Request.RouteValues["attributeDefinitionId"] = Repository.DefinitionId.Value.ToString();
        if (body is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = bytes.Length;
            context.Request.Body = new MemoryStream(bytes);
            context.Features.Set<IHttpRequestBodyDetectionFeature>(new BodyFeature(bytes.Length > 0));
        }
        context.Response.Body = new MemoryStream();
        await endpoint.RequestDelegate!(context);
        return context;
    }

    public static async Task<JsonDocument> Read(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private sealed class BodyFeature(bool canHaveBody) : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => canHaveBody;
    }

    internal sealed class RepositoryFake : IProductRepository, IProductTypeRepository
    {
        public RepositoryFake(AttributeDataType dataType, AttributeScope scope)
        {
            ProductType = ProductType.Create("Type");
            ProductType.AddAttribute(DefinitionId, AttributeCode.Create("value"), "Value", dataType, false, false, scope);
            Product = Product.Create("Product", ProductType.Id, "First");
            Variant = Product.Variants.Single();
            Other = Product.AddVariant("Other");
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
        }
        public ProductType ProductType { get; }
        public Product Product { get; }
        public ProductVariant Variant { get; }
        public ProductVariant Other { get; }
        public AttributeDefinitionId DefinitionId { get; } = AttributeDefinitionId.New();
        public ProductConcurrencyToken Token { get; private set; }
        public int ReadCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Assert.Equal(Product.Id, id);
            cancellationToken.ThrowIfCancellationRequested();
            ReadCalls++;
            return Task.FromResult<ProductSnapshot?>(new(Product, Token));
        }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            Assert.Equal(ProductType.Id, id);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ProductType?>(ProductType);
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken)
        {
            Assert.Same(Product, product);
            Assert.Equal(Token, expectedToken);
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
            return Task.FromResult(Token);
        }
    }
}
