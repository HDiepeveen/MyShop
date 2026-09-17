using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetProductAttributeValue.SetProductAttributeValue;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class SetProductAttributeValueEndpointTests
{
    [Fact]
    public void MapSetProductAttributeValue_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => SetProductAttributeValueEndpoint.MapSetProductAttributeValue(null!));

    [Fact]
    public void MapSetProductAttributeValue_MapsNamedPutRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapSetProductAttributeValue());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/attributes/{attributeDefinitionId:guid}",
            endpoint.RoutePattern.RawText);
        Assert.Equal("SetProductAttributeValue", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PUT"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_SetsValueAndReturnsNoContent()
    {
        var scenario = new Scenario();

        var result = await scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Cotton\""));

        Assert.IsType<NoContent>(result.Result);
        var value = Assert.IsType<TextAttributeValue>(Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal("Cotton", value.Value);
        Assert.Equal(scenario.DefinitionId, value.AttributeDefinitionId);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData("product", "Product not found")]
    [InlineData("productType", "Product type not found")]
    [InlineData("definition", "Attribute definition not found")]
    public async Task ExecuteAsync_WhenDependencyDoesNotExist_ReturnsSpecificNotFound(
        string missing,
        string expectedTitle)
    {
        var scenario = new Scenario();
        if (missing == "product") scenario.ProductIsMissing = true;
        if (missing == "productType") scenario.ProductTypeIsMissing = true;
        var definitionId = missing == "definition" ? AttributeDefinitionId.New() : scenario.DefinitionId;

        var result = await scenario.ExecuteAsync(
            Request(AttributeDataType.Text, "\"Cotton\""), definitionId);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(expectedTitle, notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAttributeHasVariantScope_ReturnsConflict()
    {
        var scenario = new Scenario(scope: AttributeScope.Variant);

        var result = await scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Cotton\""));

        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("Wrong attribute scope", conflict.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataTypeDoesNotMatch_ReturnsConflict()
    {
        var scenario = new Scenario(AttributeDataType.Integer);

        var result = await scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Cotton\""));

        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("Wrong attribute data type", conflict.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExecuteAsync_WhenIdIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        bool emptyDefinitionId)
    {
        var scenario = new Scenario();

        var result = await SetProductAttributeValueEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyDefinitionId ? Guid.Empty : scenario.DefinitionId.Value,
            Request(AttributeDataType.Text, "\"Cotton\""),
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product attribute value", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValueIsInvalid_ReturnsBadRequestBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        var result = await scenario.ExecuteAsync(Request(AttributeDataType.Text, "42"));

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product attribute value", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConflict()
    {
        var scenario = new Scenario { HasConcurrencyConflict = true };

        var result = await scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Cotton\""));

        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("Product was modified", conflict.Value!.Title);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Cotton\""), cancellationToken: source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    private static AttributeValueRequest Request(AttributeDataType dataType, string json)
    {
        using var document = JsonDocument.Parse(json);
        return new AttributeValueRequest(dataType, document.RootElement.Clone());
    }

    private sealed class Scenario
    {
        public Scenario(
            AttributeDataType dataType = AttributeDataType.Text,
            AttributeScope scope = AttributeScope.Product)
        {
            ProductType = ProductType.Create("Clothing");
            ProductType.AddAttribute(
                DefinitionId,
                AttributeCode.Create("material"),
                "Material",
                dataType,
                false,
                false,
                scope);
            Product = Product.Create("Shirt", ProductType.Id, "Medium");
            Products = new ProductRepositoryFake(this);
            ProductTypes = new ProductTypeRepositoryFake(this);
            UseCase = new UseCase(Products, ProductTypes);
        }

        public ProductType ProductType { get; }
        public Product Product { get; }
        public AttributeDefinitionId DefinitionId { get; } = AttributeDefinitionId.New();
        public bool ProductIsMissing { get; set; }
        public bool ProductTypeIsMissing { get; set; }
        public bool HasConcurrencyConflict { get; set; }
        public ProductRepositoryFake Products { get; }
        public ProductTypeRepositoryFake ProductTypes { get; }
        public UseCase UseCase { get; }

        public Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>>
            ExecuteAsync(
                AttributeValueRequest request,
                AttributeDefinitionId? definitionId = null,
                CancellationToken cancellationToken = default) =>
            SetProductAttributeValueEndpoint.ExecuteAsync(
                Product.Id.Value,
                (definitionId ?? DefinitionId).Value,
                request,
                UseCase,
                cancellationToken);
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        private readonly ProductConcurrencyToken _readToken =
            ProductConcurrencyToken.Create(scenario.Product.Id, Guid.NewGuid());

        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.ProductIsMissing
                ? null
                : new ProductSnapshot(scenario.Product, _readToken));
        }

        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken)
        {
            SaveCalls++;
            if (scenario.HasConcurrencyConflict)
                return Task.FromException<ProductConcurrencyToken>(new ProductConcurrencyException(product.Id));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }

    private sealed class ProductTypeRepositoryFake(Scenario scenario) : IProductTypeRepository
    {
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.ProductTypeIsMissing ? null : scenario.ProductType);
        }
    }
}
