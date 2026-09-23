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
    public void MapGetProduct_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => GetProductEndpoint.MapGetProduct(null!));

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

    [Fact]
    public async Task ExecuteAsync_NullUseCase_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetProductEndpoint.ExecuteAsync(
            Guid.NewGuid(), null!, CancellationToken.None));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_MapsAbsentAndZeroBasePriceDistinctly(bool hasPrice)
    {
        var scenario = new Scenario();
        if (hasPrice)
            scenario.Product.SetVariantPrice(scenario.Variant.Id, Money.Create(0m, "EUR"));

        var result = await GetProductEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.UseCase, CancellationToken.None);
        var variant = Assert.Single(Assert.IsType<Ok<GetProductResponse>>(result.Result).Value!.Variants);
        if (hasPrice)
            Assert.Equal(new MoneyResponse(0m, "EUR"), variant.Price);
        else
            Assert.Null(variant.Price);
    }

    [Fact]
    public async Task ExecuteAsync_PricesBelongToCorrectVariantsAndRemainBasePrices()
    {
        var scenario = new Scenario();
        var second = scenario.Product.AddVariant("Second");
        scenario.Product.SetVariantPrice(scenario.Variant.Id, Money.Create(100m, "EUR"));
        scenario.Product.SetVariantPrice(second.Id, Money.Create(25m, "USD"));
        scenario.Product.AddVariantPriceRule(scenario.Variant.Id,
            PriceRule.Create("Sale", PriceAdjustmentType.PercentageDiscount, 50m, 1));

        var result = await GetProductEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.UseCase, CancellationToken.None);
        var variants = Assert.IsType<Ok<GetProductResponse>>(result.Result).Value!.Variants;
        Assert.Equal(new MoneyResponse(100m, "EUR"), variants.Single(v => v.Id == scenario.Variant.Id.Value).Price);
        Assert.Equal(new MoneyResponse(25m, "USD"), variants.Single(v => v.Id == second.Id.Value).Price);
    }

    [Fact]
    public async Task ExecuteAsync_MapsAllRulesWithStableOrderAndKeepsVariantsSeparate()
    {
        var scenario = new Scenario();
        var second = scenario.Product.AddVariant("Second");
        var at = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));
        var low = PriceRule.Create("Low", PriceAdjustmentType.FixedDiscount, 2m, 0);
        var high = PriceRule.Create("Scheduled", PriceAdjustmentType.PercentageDiscount, 25m, 5, at, at.AddDays(1));
        var tied = PriceRule.Create("Tied", PriceAdjustmentType.FixedDiscount, 3m, 5);
        scenario.Product.AddVariantPriceRule(scenario.Variant.Id, low);
        scenario.Product.AddVariantPriceRule(scenario.Variant.Id, high);
        scenario.Product.AddVariantPriceRule(scenario.Variant.Id, tied);

        var result = await GetProductEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.UseCase, CancellationToken.None);
        var variants = Assert.IsType<Ok<GetProductResponse>>(result.Result).Value!.Variants;
        var rules = variants.Single(v => v.Id == scenario.Variant.Id.Value).PriceRules;

        Assert.Equal(new[] { high.Id, tied.Id }.Order().Append(low.Id), rules.Select(rule => rule.Id));
        var response = rules.Single(rule => rule.Id == high.Id);
        Assert.Equal("Scheduled", response.Name);
        Assert.Equal(1, response.AdjustmentType);
        Assert.Equal(25m, response.Value);
        Assert.Equal(5, response.Priority);
        Assert.Equal(at, response.StartsAt);
        Assert.Equal(at.Offset, response.StartsAt!.Value.Offset);
        Assert.Equal(at.AddDays(1), response.EndsAt);
        Assert.Empty(variants.Single(v => v.Id == second.Id.Value).PriceRules);
        Assert.Equal(3, scenario.Variant.PriceRules.Count);
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
