using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ClearProductVariantPrice.ClearProductVariantPrice;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class ClearProductVariantPriceEndpointTests
{
    [Fact]
    public void MapClearProductVariantPrice_RejectsNullBuilder() =>
        Assert.Throws<ArgumentNullException>(() => ClearProductVariantPriceEndpoint.MapClearProductVariantPrice(null!));

    [Fact]
    public void MapClearProductVariantPrice_MapsNamedDeleteRoute()
    {
        var app = WebApplication.CreateBuilder().Build();

        var returned = app.MapClearProductVariantPrice();

        Assert.Same(app, returned);
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(((IEndpointRouteBuilder)app).DataSources).Endpoints.Single());
        Assert.Equal("/api/products/{productId:guid}/variants/{variantId:guid}/price", endpoint.RoutePattern.RawText);
        Assert.Equal("ClearProductVariantPrice", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["DELETE"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_ClearsPriceAndReturnsNoContent()
    {
        var scenario = new Scenario();

        var result = await ClearProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.Variant.Id.Value, scenario.UseCase, CancellationToken.None);

        Assert.IsType<NoContent>(result.Result);
        Assert.Null(scenario.Variant.Price);
        Assert.Equal(1, scenario.Repository.SaveCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WhenProductOrVariantMissing_ReturnsSpecificNotFound(bool productMissing)
    {
        var scenario = new Scenario { ProductMissing = productMissing };

        var result = await ClearProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value,
            productMissing ? scenario.Variant.Id.Value : Guid.NewGuid(),
            scenario.UseCase,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(productMissing ? "Product not found" : "Product variant not found", notFound.Value!.Title);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExecuteAsync_WithEmptyIdentifier_ReturnsBadRequest(bool emptyProductId, bool emptyVariantId)
    {
        var scenario = new Scenario();

        var result = await ClearProductVariantPriceEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyVariantId ? Guid.Empty : scenario.Variant.Id.Value,
            scenario.UseCase,
            CancellationToken.None);

        Assert.Equal("Invalid product variant price", Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConflict()
    {
        var scenario = new Scenario { Conflict = true };

        var result = await ClearProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.Variant.Id.Value, scenario.UseCase, CancellationToken.None);

        Assert.Equal("Product was modified", Assert.IsType<Conflict<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullUseCase() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => ClearProductVariantPriceEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));

    [Fact]
    public async Task ExecuteAsync_HonorsCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ClearProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.Variant.Id.Value, scenario.UseCase, source.Token));
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPriceAlreadyAbsent_ReturnsNoContentWithoutSave()
    {
        var scenario = new Scenario();
        scenario.Product.ClearVariantPrice(scenario.Variant.Id);
        var result = await ClearProductVariantPriceEndpoint.ExecuteAsync(
            scenario.Product.Id.Value, scenario.Variant.Id.Value, scenario.UseCase, CancellationToken.None);
        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(0, scenario.Repository.SaveCalls);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            Variant = Product.Variants.Single();
            Product.SetVariantPrice(Variant.Id, Money.Create(9.99m, "EUR"));
            Repository = new RepositoryFake(this);
            UseCase = new UseCase(Repository);
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public RepositoryFake Repository { get; }
        public UseCase UseCase { get; }
        public bool ProductMissing { get; init; }
        public bool Conflict { get; init; }
    }

    private sealed class RepositoryFake(Scenario scenario) : IProductRepository
    {
        private readonly ProductConcurrencyToken _token = ProductConcurrencyToken.Create(scenario.Product.Id, Guid.NewGuid());
        public int SaveCalls { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.ProductMissing ? null : new ProductSnapshot(scenario.Product, _token));
        }

        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken)
        {
            Assert.Same(_token, expectedToken);
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            return scenario.Conflict
                ? Task.FromException<ProductConcurrencyToken>(new ProductConcurrencyException(product.Id))
                : Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
    }
}
