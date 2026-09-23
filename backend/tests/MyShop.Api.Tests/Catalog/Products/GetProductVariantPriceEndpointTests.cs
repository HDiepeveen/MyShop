using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductVariantPrice.GetProductVariantPrice;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class GetProductVariantPriceEndpointTests
{
    private static readonly DateTimeOffset At = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public async Task ExecuteAsync_ReturnsBaseAndCalculatedPriceWithoutSaving()
    {
        var repository = new RepositoryFake();
        var variant = repository.Product.Variants.Single();
        repository.Product.SetVariantPrice(variant.Id, Money.Create(100m, "EUR"));
        repository.Product.AddVariantPriceRule(variant.Id, PriceRule.Create("Sale", PriceAdjustmentType.PercentageDiscount, 25m, 5, At, At));
        var result = await GetProductVariantPriceEndpoint.ExecuteAsync(
            repository.Product.Id.Value, variant.Id.Value, At, new UseCase(repository), CancellationToken.None);
        var response = Assert.IsType<Ok<ProductVariantPriceResponse>>(result.Result).Value!;
        Assert.Equal(variant.PriceRules.Single().Id, response.AppliedPriceRuleId);
        Assert.Equal(100m, response.BaseAmount);
        Assert.Equal(75m, response.Amount);
        Assert.Equal("EUR", response.Currency);
        Assert.Equal(At, response.At);
        Assert.Equal(repository.Token.Revision, response.Revision);
    }

    [Theory]
    [InlineData("product", "Product not found")]
    [InlineData("variant", "Product variant not found")]
    public async Task ExecuteAsync_ReturnsSpecificNotFound(string missing, string title)
    {
        var repository = new RepositoryFake { Missing = missing == "product" };
        var result = await GetProductVariantPriceEndpoint.ExecuteAsync(
            repository.Product.Id.Value, Guid.NewGuid(), At, new UseCase(repository), CancellationToken.None);
        Assert.Equal(title, Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_UnpricedVariantReturnsConflict()
    {
        var repository = new RepositoryFake();
        var result = await GetProductVariantPriceEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.Product.Variants.Single().Id.Value, At,
            new UseCase(repository), CancellationToken.None);
        Assert.Equal("Variant base price is not set", Assert.IsType<Conflict<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_EmptyIdentityReturnsBadRequest(bool product)
    {
        var repository = new RepositoryFake();
        var result = await GetProductVariantPriceEndpoint.ExecuteAsync(
            product ? Guid.Empty : repository.Product.Id.Value,
            product ? repository.Product.Variants.Single().Id.Value : Guid.Empty,
            At, new UseCase(repository), CancellationToken.None);
        Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal(0, repository.ReadCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?at=invalid")]
    public async Task BoundEndpoint_RequiresValidAtQueryBeforeRepositoryAccess(string query)
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        var endpoint = GetEndpoint(app);
        var context = Context(app, repository, query);

        await endpoint.RequestDelegate!(context);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task BoundEndpoint_ParsesOffsetAndSerializesQuote()
    {
        var repository = new RepositoryFake();
        repository.Product.SetVariantPrice(repository.Product.Variants.Single().Id, Money.Create(20m, "EUR"));
        await using var app = CreateApp(repository);
        var endpoint = GetEndpoint(app);
        var context = Context(app, repository, "?at=" + Uri.EscapeDataString(At.ToString("O")));

        await endpoint.RequestDelegate!(context);

        Assert.Equal(200, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ProductVariantPriceResponse>(
            context.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(response);
        Assert.Null(response.AppliedPriceRuleId);
        Assert.Equal(20m, response.Amount);
        Assert.Equal(At.Offset, response.At.Offset);
        Assert.Equal(At, response.At);
        Assert.Equal("GET", Assert.Single(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods));
        Assert.Equal("GetProductVariantPrice", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal("/api/products/{productId:guid}/variants/{variantId:guid}/price", endpoint.RoutePattern.RawText);
    }

    [Fact]
    public async Task BoundEndpoint_SerializesAppliedPriceRuleId()
    {
        var repository = new RepositoryFake();
        var variant = repository.Product.Variants.Single();
        repository.Product.SetVariantPrice(variant.Id, Money.Create(100m, "EUR"));
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 20m, 1);
        repository.Product.AddVariantPriceRule(variant.Id, rule);
        await using var app = CreateApp(repository);
        var context = Context(app, repository, "?at=" + Uri.EscapeDataString(At.ToString("O")));

        await GetEndpoint(app).RequestDelegate!(context);

        Assert.Equal(200, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(rule.Id, json.RootElement.GetProperty("appliedPriceRuleId").GetGuid());
        Assert.Equal(80m, json.RootElement.GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task ExecuteAsync_HonorsCancellation()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetProductVariantPriceEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.Product.Variants.Single().Id.Value, At,
            new UseCase(repository), source.Token));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public void Map_RejectsNullBuilder() =>
        Assert.Throws<ArgumentNullException>(() => GetProductVariantPriceEndpoint.MapGetProductVariantPrice(null!));

    [Fact]
    public async Task ExecuteAsync_RejectsNullUseCase() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetProductVariantPriceEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), At, null!, CancellationToken.None));

    private static WebApplication CreateApp(RepositoryFake repository)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(new UseCase(repository));
        var app = builder.Build();
        Assert.Same(app, app.MapGetProductVariantPrice());
        return app;
    }

    private static RouteEndpoint GetEndpoint(WebApplication app) =>
        Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());

    private static DefaultHttpContext Context(WebApplication app, RepositoryFake repository, string query)
    {
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = "GET";
        context.Request.RouteValues["productId"] = repository.Product.Id.Value.ToString();
        context.Request.RouteValues["variantId"] = repository.Product.Variants.Single().Id.Value.ToString();
        context.Request.QueryString = new QueryString(query);
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class RepositoryFake : IProductRepository
    {
        public RepositoryFake() => Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "Variant");
        public ProductConcurrencyToken Token { get; }
        public bool Missing { get; init; }
        public int ReadCalls { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Assert.Equal(Product.Id, id);
            cancellationToken.ThrowIfCancellationRequested();
            ReadCalls++;
            return Task.FromResult(Missing ? null : new ProductSnapshot(Product, Token));
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
    }
}
