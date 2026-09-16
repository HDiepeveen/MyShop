using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProduct.GetProduct;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class GetProductEndpointTests
{
    [Fact]
    public void MapGetProduct_MapsNamedGetRouteAndReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var returned = app.MapGetProduct();

        Assert.Same(app, returned);
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal("/api/products/{productId:guid}", endpoint.RoutePattern.RawText);
        Assert.Equal("GetProduct", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductExists_MapsCompleteSnapshot()
    {
        var scenario = new Scenario();

        var result = await GetProductEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.UseCase,
            CancellationToken.None);

        var ok = Assert.IsType<Ok<GetProductResponse>>(result.Result);
        var response = Assert.IsType<GetProductResponse>(ok.Value);
        Assert.Equal(scenario.Product.Id.Value, response.Id);
        Assert.Equal(scenario.Product.ProductTypeId.Value, response.ProductTypeId);
        Assert.Equal(scenario.Product.Name, response.Name);
        Assert.Equal([scenario.CategoryId.Value], response.CategoryIds);
        Assert.Equal(Assert.IsType<ProductSnapshot>(scenario.Snapshot).ConcurrencyToken.Revision, response.Revision);

        var productAttributes = response.AttributeValues.ToDictionary(attribute => attribute.DataType);
        Assert.Equal(7, productAttributes.Count);
        Assert.Equal("Cotton", productAttributes["Text"].Value);
        Assert.Equal(42L, productAttributes["Integer"].Value);
        Assert.Equal(12.50m, productAttributes["Decimal"].Value);
        Assert.Equal(true, productAttributes["Boolean"].Value);
        Assert.Equal(new DateOnly(2026, 9, 16), productAttributes["Date"].Value);
        Assert.Equal("Large", productAttributes["Choice"].Value);
        Assert.Equal(
            ["Red", "Blue"],
            Assert.IsType<string[]>(productAttributes["MultiChoice"].Value));

        var variant = Assert.Single(response.Variants);
        Assert.Equal(scenario.Variant.Id.Value, variant.Id);
        Assert.Equal(scenario.Variant.Name, variant.Name);
        Assert.Equal("SKU-1", variant.Sku);
        var variantAttribute = Assert.Single(variant.AttributeValues);
        Assert.Equal(scenario.VariantAttributeId.Value, variantAttribute.AttributeDefinitionId);
        Assert.Equal("MultiChoice", variantAttribute.DataType);
        Assert.Equal(["Red", "Blue"], Assert.IsType<string[]>(variantAttribute.Value));
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var scenario = new Scenario { Snapshot = null };

        var result = await GetProductEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            scenario.UseCase,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal("Product not found", notFound.Value!.Title);
        Assert.Contains(scenario.Product.Id.Value.ToString(), notFound.Value.Detail);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIdIsEmpty_ReturnsBadRequestWithoutRepositoryAccess()
    {
        var scenario = new Scenario();

        var result = await GetProductEndpoint.ExecuteAsync(
            Guid.Empty,
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product ID", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GetProductEndpoint.ExecuteAsync(
                scenario.Product.Id.Value,
                scenario.UseCase,
                source.Token));
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Product.Create("Shirt", ProductTypeId.New(), "Medium");
            Variant = Product.Variants.Single();
            Product.SetVariantSku(Variant.Id, Sku.Create("sku-1"));

            CategoryId = CategoryId.New();
            Product.AssignToCategory(CategoryId);
            Product.SetAttributeValue(TextAttributeValue.Create(AttributeDefinitionId.New(), "Cotton"));
            Product.SetAttributeValue(IntegerAttributeValue.Create(AttributeDefinitionId.New(), 42));
            Product.SetAttributeValue(DecimalAttributeValue.Create(AttributeDefinitionId.New(), 12.50m));
            Product.SetAttributeValue(BooleanAttributeValue.Create(AttributeDefinitionId.New(), true));
            Product.SetAttributeValue(DateAttributeValue.Create(
                AttributeDefinitionId.New(),
                new DateOnly(2026, 9, 16)));
            Product.SetAttributeValue(ChoiceAttributeValue.Create(
                AttributeDefinitionId.New(),
                ChoiceValue.Create("Large")));
            Product.SetAttributeValue(MultiChoiceAttributeValue.Create(
                AttributeDefinitionId.New(),
                [ChoiceValue.Create("Red"), ChoiceValue.Create("Blue")]));
            VariantAttributeId = AttributeDefinitionId.New();
            Product.SetVariantAttributeValue(
                Variant.Id,
                MultiChoiceAttributeValue.Create(
                    VariantAttributeId,
                    [ChoiceValue.Create("Red"), ChoiceValue.Create("Blue")]));

            Snapshot = new ProductSnapshot(
                Product,
                ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid()));
            Products = new ProductRepositoryFake(this);
            UseCase = new UseCase(Products);
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public CategoryId CategoryId { get; }
        public AttributeDefinitionId VariantAttributeId { get; }
        public ProductSnapshot? Snapshot { get; set; }
        public ProductRepositoryFake Products { get; }
        public UseCase UseCase { get; }
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        public int GetCalls { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(
            ProductId id,
            CancellationToken cancellationToken)
        {
            GetCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.Snapshot);
        }

        public Task<ProductConcurrencyToken> AddAsync(
            Product product,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
