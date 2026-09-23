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
using SetPrice = MyShop.Application.Catalog.SetProductVariantPrice.SetProductVariantPrice;
using ClearPrice = MyShop.Application.Catalog.ClearProductVariantPrice.ClearProductVariantPrice;
using AddRule = MyShop.Application.Catalog.AddProductVariantPriceRule.AddProductVariantPriceRule;
using UpdateRule = MyShop.Application.Catalog.UpdateProductVariantPriceRule.UpdateProductVariantPriceRule;
using RemoveRule = MyShop.Application.Catalog.RemoveProductVariantPriceRule.RemoveProductVariantPriceRule;
using GetRule = MyShop.Application.Catalog.GetProductVariantPriceRule.GetProductVariantPriceRule;
using ListRules = MyShop.Application.Catalog.ListProductVariantPriceRules.ListProductVariantPriceRules;
using GetPrice = MyShop.Application.Catalog.GetProductVariantPrice.GetProductVariantPrice;
using GetVariant = MyShop.Application.Catalog.GetProductVariant.GetProductVariant;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class VariantPricingHttpFlowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task BoundEndpoints_PriceRuleLifecycleKeepsReadsAndCalculatedPricesConsistent()
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        var at = DateTimeOffset.Parse("2026-01-01T00:00:00+02:00");
        var query = "?at=" + Uri.EscapeDataString(at.ToString("O"));
        var initialRevision = repository.Token.Revision;

        var set = await Send(app, repository, "SetProductVariantPrice",
            JsonSerializer.Serialize(new { amount = 100m, currency = "eur" }));
        Assert.Equal(204, set.Response.StatusCode);
        var add = await Send(app, repository, "AddProductVariantPriceRule",
            JsonSerializer.Serialize(new {
                name = " Sale ", adjustmentType = 1, value = 25m, priority = 5,
                startsAt = at, endsAt = at.AddDays(1)
            }));
        Assert.Equal(201, add.Response.StatusCode);
        var id = (await Read<AddProductVariantPriceRuleResponse>(add)).Id;
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"/api/products/{repository.Product.Id.Value}/variants/{repository.Variant.Id.Value}/price-rules/{id}",
            add.Response.Headers.Location.ToString());
        Assert.Equal(2, repository.SaveCalls);

        var get = await Send(app, repository, "GetProductVariantPriceRule", ruleId: id);
        Assert.Equal(200, get.Response.StatusCode);
        var rule = await Read<PriceRuleResponse>(get);
        Assert.Equal("Sale", rule.Name);
        Assert.Equal(at.Offset, rule.StartsAt!.Value.Offset);
        var list = await Send(app, repository, "ListProductVariantPriceRules");
        Assert.Equal(200, list.Response.StatusCode);
        var rules = await Read<ProductVariantPriceRulesResponse>(list);
        Assert.Equal(rule, Assert.Single(rules.Rules));
        Assert.Equal(repository.Token.Revision, rules.Revision);
        Assert.NotEqual(initialRevision, rules.Revision);

        var price = await Send(app, repository, "GetProductVariantPrice", query: query);
        var quote = await Read<ProductVariantPriceResponse>(price);
        Assert.Equal(200, price.Response.StatusCode);
        Assert.Equal(75m, quote.Amount);
        Assert.Equal(id, quote.AppliedPriceRuleId);
        Assert.Equal(rules.Revision, quote.Revision);
        Assert.Equal(2, repository.SaveCalls);

        var updateBody = JsonSerializer.Serialize(new { name = "Fixed", adjustmentType = 2, value = 10m, priority = 5 });
        var update = await Send(app, repository, "UpdateProductVariantPriceRule", updateBody, id);
        Assert.Equal(204, update.Response.StatusCode);
        Assert.Equal(3, repository.SaveCalls);
        var details = await Send(app, repository, "GetProductVariant");
        Assert.Equal(200, details.Response.StatusCode);
        var variant = await Read<GetProductVariantResponse>(details);
        var updatedRule = Assert.Single(variant.Variant.PriceRules);
        Assert.Equal(id, updatedRule.Id);
        Assert.Equal(10m, updatedRule.Value);
        Assert.Null(updatedRule.StartsAt);
        Assert.Null(updatedRule.EndsAt);
        Assert.Equal(new MoneyResponse(100m, "EUR"), variant.Variant.Price);
        Assert.Equal(repository.Token.Revision, variant.Revision);
        var updatedQuote = await Read<ProductVariantPriceResponse>(
            await Send(app, repository, "GetProductVariantPrice", query: query));
        Assert.Equal(90m, updatedQuote.Amount);
        Assert.Equal(id, updatedQuote.AppliedPriceRuleId);

        var revisionBeforeNoOp = repository.Token.Revision;
        Assert.Equal(204, (await Send(app, repository, "UpdateProductVariantPriceRule", updateBody, id)).Response.StatusCode);
        Assert.Equal(3, repository.SaveCalls);
        Assert.Equal(revisionBeforeNoOp, repository.Token.Revision);
        Assert.Equal(400, (await Send(app, repository, "UpdateProductVariantPriceRule",
            JsonSerializer.Serialize(new { name = "Invalid", adjustmentType = 2, value = -1m, priority = 0 }), id)).Response.StatusCode);
        Assert.Equal(3, repository.SaveCalls);
        Assert.Equal(10m, Assert.Single(repository.Variant.PriceRules).Value);

        var other = await Send(app, repository, "GetProductVariantPriceRule", ruleId: id, variantId: repository.Other.Id.Value);
        Assert.Equal(404, other.Response.StatusCode);
        Assert.Equal(204, (await Send(app, repository, "RemoveProductVariantPriceRule", ruleId: id)).Response.StatusCode);
        Assert.Equal(4, repository.SaveCalls);
        Assert.Equal(404, (await Send(app, repository, "GetProductVariantPriceRule", ruleId: id)).Response.StatusCode);
        Assert.Empty((await Read<ProductVariantPriceRulesResponse>(
            await Send(app, repository, "ListProductVariantPriceRules"))).Rules);
        var baseQuote = await Read<ProductVariantPriceResponse>(
            await Send(app, repository, "GetProductVariantPrice", query: query));
        Assert.Equal(100m, baseQuote.Amount);
        Assert.Null(baseQuote.AppliedPriceRuleId);
        Assert.Equal(repository.Token.Revision, baseQuote.Revision);
        Assert.Equal(4, repository.SaveCalls);

        Assert.Equal(204, (await Send(app, repository, "ClearProductVariantPrice")).Response.StatusCode);
        Assert.Equal(409, (await Send(app, repository, "GetProductVariantPrice", query: query)).Response.StatusCode);
        Assert.Equal(5, repository.SaveCalls);
        Assert.Null(repository.Other.Price);
        Assert.Empty(repository.Other.PriceRules);
    }

    [Theory]
    [InlineData("AddProductVariantPriceRule", "{")]
    [InlineData("AddProductVariantPriceRule", "")]
    [InlineData("UpdateProductVariantPriceRule", "{")]
    [InlineData("UpdateProductVariantPriceRule", "")]
    public async Task BoundEndpoints_RejectInvalidOrMissingJsonBeforeRepositoryRead(string endpoint, string body)
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        var response = await Send(app, repository, endpoint, body, Guid.NewGuid());
        Assert.Equal(400, response.Response.StatusCode);
        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
    }

    private static WebApplication CreateApp(RepositoryFake repository)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IProductRepository>(repository);
        builder.Services.AddScoped<SetPrice>();
        builder.Services.AddScoped<ClearPrice>();
        builder.Services.AddScoped<AddRule>();
        builder.Services.AddScoped<UpdateRule>();
        builder.Services.AddScoped<RemoveRule>();
        builder.Services.AddScoped<GetRule>();
        builder.Services.AddScoped<ListRules>();
        builder.Services.AddScoped<GetPrice>();
        builder.Services.AddScoped<GetVariant>();
        var app = builder.Build();
        app.MapSetProductVariantPrice();
        app.MapClearProductVariantPrice();
        app.MapAddProductVariantPriceRule();
        app.MapUpdateProductVariantPriceRule();
        app.MapRemoveProductVariantPriceRule();
        app.MapGetProductVariantPriceRule();
        app.MapListProductVariantPriceRules();
        app.MapGetProductVariantPrice();
        app.MapGetProductVariant();
        return app;
    }

    private static async Task<DefaultHttpContext> Send(WebApplication app, RepositoryFake repository,
        string name, string? body = null, Guid? ruleId = null, string? query = null, Guid? variantId = null)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = Assert.Single(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
        context.Request.RouteValues["productId"] = repository.Product.Id.Value.ToString();
        context.Request.RouteValues["variantId"] = (variantId ?? repository.Variant.Id.Value).ToString();
        if (ruleId is not null) context.Request.RouteValues["priceRuleId"] = ruleId.Value.ToString();
        context.Request.QueryString = new QueryString(query ?? "");
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

    private static async Task<T> Read<T>(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        var value = await JsonSerializer.DeserializeAsync<T>(context.Response.Body, JsonOptions);
        Assert.NotNull(value);
        return value;
    }

    private sealed class BodyFeature(bool canHaveBody) : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => canHaveBody;
    }

    private sealed class RepositoryFake : IProductRepository
    {
        public RepositoryFake()
        {
            Variant = Product.Variants.Single();
            Other = Product.AddVariant("Other");
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
        }
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "Variant");
        public ProductVariant Variant { get; }
        public ProductVariant Other { get; }
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
