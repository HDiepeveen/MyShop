using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetVariantAttributeValue.SetVariantAttributeValue;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class SetVariantAttributeValueEndpointTests
{
    [Fact]
    public void MapSetVariantAttributeValue_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => SetVariantAttributeValueEndpoint.MapSetVariantAttributeValue(null!));

    [Fact]
    public void MapSetVariantAttributeValue_MapsNamedPutRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapSetVariantAttributeValue());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/variants/{variantId:guid}/attributes/{attributeDefinitionId:guid}",
            endpoint.RoutePattern.RawText);
        Assert.Equal("SetVariantAttributeValue", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PUT"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Theory]
    [InlineData(AttributeDataType.Text, "\"Blue\"")]
    [InlineData(AttributeDataType.Integer, "42")]
    [InlineData(AttributeDataType.Decimal, "19.95")]
    [InlineData(AttributeDataType.Boolean, "true")]
    [InlineData(AttributeDataType.Date, "\"2026-09-17\"")]
    [InlineData(AttributeDataType.Choice, "\"blue\"")]
    [InlineData(AttributeDataType.MultiChoice, "[\"blue\",\"red\"]")]
    public async Task ExecuteAsync_MapsEveryValueTypeAndReturnsNoContent(
        AttributeDataType dataType,
        string json)
    {
        var scenario = new Scenario(dataType);

        var result = await scenario.ExecuteAsync(Request(dataType, json));

        Assert.IsType<NoContent>(result.Result);
        var value = Assert.Single(scenario.Variant.AttributeValues);
        Assert.Equal(dataType, value.DataType);
        Assert.Equal(scenario.DefinitionId, value.AttributeDefinitionId);
        AssertMappedValue(value, dataType);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData("product", "Product not found")]
    [InlineData("variant", "Product variant not found")]
    [InlineData("productType", "Product type not found")]
    [InlineData("definition", "Attribute definition not found")]
    public async Task ExecuteAsync_WhenDependencyDoesNotExist_ReturnsSpecificNotFound(
        string missing,
        string expectedTitle)
    {
        var scenario = new Scenario();
        if (missing == "product") scenario.ProductIsMissing = true;
        if (missing == "productType") scenario.ProductTypeIsMissing = true;
        var variantId = missing == "variant" ? ProductVariantId.New() : scenario.Variant.Id;
        var definitionId = missing == "definition" ? AttributeDefinitionId.New() : scenario.DefinitionId;

        var result = await scenario.ExecuteAsync(
            Request(AttributeDataType.Text, "\"Blue\""), variantId, definitionId);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(expectedTitle, notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAttributeHasProductScope_ReturnsConflict()
    {
        var scenario = new Scenario(scope: AttributeScope.Product);

        var result = await scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Blue\""));

        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("Wrong attribute scope", conflict.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataTypeDoesNotMatch_ReturnsConflict()
    {
        var scenario = new Scenario(AttributeDataType.Integer);

        var result = await scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Blue\""));

        var conflict = Assert.IsType<Conflict<ProblemDetails>>(result.Result);
        Assert.Equal("Wrong attribute data type", conflict.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(AttributeDataType.Text, "42")]
    [InlineData(AttributeDataType.Integer, "1.5")]
    [InlineData(AttributeDataType.Decimal, "true")]
    [InlineData(AttributeDataType.Boolean, "\"true\"")]
    [InlineData(AttributeDataType.Date, "\"17-09-2026\"")]
    [InlineData(AttributeDataType.MultiChoice, "[\"blue\",42]")]
    [InlineData((AttributeDataType)99, "\"value\"")]
    public async Task ExecuteAsync_WhenValueIsInvalid_ReturnsBadRequest(
        AttributeDataType dataType,
        string json)
    {
        var scenario = new Scenario();

        var result = await scenario.ExecuteAsync(Request(dataType, json));

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product variant attribute value", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task ExecuteAsync_WhenIdIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        bool emptyVariantId,
        bool emptyDefinitionId)
    {
        var scenario = new Scenario();

        var result = await SetVariantAttributeValueEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyVariantId ? Guid.Empty : scenario.Variant.Id.Value,
            emptyDefinitionId ? Guid.Empty : scenario.DefinitionId.Value,
            Request(AttributeDataType.Text, "\"Blue\""),
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product variant attribute value", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConflict()
    {
        var scenario = new Scenario { HasConcurrencyConflict = true };

        var result = await scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Blue\""));

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
            scenario.ExecuteAsync(Request(AttributeDataType.Text, "\"Blue\""), cancellationToken: source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    private static AttributeValueRequest Request(AttributeDataType dataType, string json)
    {
        using var document = JsonDocument.Parse(json);
        return new AttributeValueRequest(dataType, document.RootElement.Clone());
    }

    private static void AssertMappedValue(AttributeValue value, AttributeDataType dataType)
    {
        switch (dataType)
        {
            case AttributeDataType.Text:
                Assert.Equal("Blue", Assert.IsType<TextAttributeValue>(value).Value);
                break;
            case AttributeDataType.Integer:
                Assert.Equal(42, Assert.IsType<IntegerAttributeValue>(value).Value);
                break;
            case AttributeDataType.Decimal:
                Assert.Equal(19.95m, Assert.IsType<DecimalAttributeValue>(value).Value);
                break;
            case AttributeDataType.Boolean:
                Assert.True(Assert.IsType<BooleanAttributeValue>(value).Value);
                break;
            case AttributeDataType.Date:
                Assert.Equal(new DateOnly(2026, 9, 17), Assert.IsType<DateAttributeValue>(value).Value);
                break;
            case AttributeDataType.Choice:
                Assert.Equal("blue", Assert.IsType<ChoiceAttributeValue>(value).Value.Value);
                break;
            case AttributeDataType.MultiChoice:
                Assert.Equal(["blue", "red"], Assert.IsType<MultiChoiceAttributeValue>(value).Values
                    .Select(choice => choice.Value));
                break;
        }
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => SetVariantAttributeValueEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!, null!, CancellationToken.None));

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => SetVariantAttributeValueEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new(AttributeDataType.Text, default), null!, CancellationToken.None));

    private sealed class Scenario
    {
        public Scenario(
            AttributeDataType dataType = AttributeDataType.Text,
            AttributeScope scope = AttributeScope.Variant)
        {
            ProductType = ProductType.Create("Clothing");
            ProductType.AddAttribute(
                DefinitionId,
                AttributeCode.Create("attribute"),
                "Attribute",
                dataType,
                false,
                false,
                scope);
            Product = Product.Create("Shirt", ProductType.Id, "Medium");
            Variant = Product.Variants.Single();
            Products = new ProductRepositoryFake(this);
            ProductTypes = new ProductTypeRepositoryFake(this);
            UseCase = new UseCase(Products, ProductTypes);
        }

        public ProductType ProductType { get; }
        public Product Product { get; }
        public ProductVariant Variant { get; }
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
                ProductVariantId? variantId = null,
                AttributeDefinitionId? definitionId = null,
                CancellationToken cancellationToken = default) =>
            SetVariantAttributeValueEndpoint.ExecuteAsync(
                Product.Id.Value,
                (variantId ?? Variant.Id).Value,
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
