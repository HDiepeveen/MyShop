using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateProduct.CreateProduct;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class CreateProductEndpointTests
{
    [Fact]
    public void MapCreateProduct_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CreateProductEndpoint.MapCreateProduct(null!));

    [Fact]
    public void MapCreateProduct_MapsNamedPostRouteAndReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var returned = app.MapCreateProduct();

        Assert.Same(app, returned);
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal("/api/products", endpoint.RoutePattern.RawText);
        Assert.Equal("CreateProduct", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["POST"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public void MapCreateProduct_RejectsNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CreateProductEndpoint.MapCreateProduct(null!));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesProductAndReturnsCreatedResponse()
    {
        var scenario = new Scenario();

        var result = await CreateProductEndpoint.ExecuteAsync(
            scenario.Request,
            scenario.UseCase,
            CancellationToken.None);

        var created = Assert.IsType<Created<CreateProductResponse>>(result.Result);
        var response = Assert.IsType<CreateProductResponse>(created.Value);
        var product = Assert.IsType<Product>(scenario.Products.AddedProduct);
        var variant = product.Variants.Single();
        Assert.Equal($"/api/products/{product.Id.Value}", created.Location);
        Assert.Equal(product.Id.Value, response.Id);
        Assert.Equal(scenario.Request.ProductTypeId, response.ProductTypeId);
        Assert.Equal(scenario.Request.Name, response.Name);
        Assert.Equal(variant.Id.Value, response.InitialVariantId);
        Assert.Equal(scenario.Request.InitialVariantName, response.InitialVariantName);
        Assert.Equal(scenario.Products.ReturnedToken!.Revision, response.Revision);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductTypeDoesNotExist_ReturnsNotFound()
    {
        var scenario = new Scenario { ProductType = null };

        var result = await CreateProductEndpoint.ExecuteAsync(
            scenario.Request,
            scenario.UseCase,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal("Product type not found", notFound.Value!.Title);
        Assert.Contains(scenario.Request.ProductTypeId.ToString(), notFound.Value.Detail);
        Assert.Null(scenario.Products.AddedProduct);
    }

    [Theory]
    [InlineData(true, "Product", "Standard")]
    [InlineData(false, " ", "Standard")]
    [InlineData(false, "Product", " ")]
    public async Task ExecuteAsync_WhenRequestIsInvalid_ReturnsBadRequest(
        bool emptyProductTypeId,
        string name,
        string initialVariantName)
    {
        var scenario = new Scenario();
        var request = scenario.Request with
        {
            ProductTypeId = emptyProductTypeId ? Guid.Empty : scenario.Request.ProductTypeId,
            Name = name,
            InitialVariantName = initialVariantName
        };

        var result = await CreateProductEndpoint.ExecuteAsync(
            request,
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product", badRequest.Value!.Title);
        Assert.NotEmpty(badRequest.Value.Detail!);
        Assert.Null(scenario.Products.AddedProduct);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateProductEndpoint.ExecuteAsync(scenario.Request, scenario.UseCase, source.Token));

        Assert.Null(scenario.Products.AddedProduct);
    }

    private sealed class Scenario : IProductTypeRepository
    {
        public Scenario()
        {
            ProductType = ProductType.Create("Type");
            Products = new ProductRepositoryFake();
            UseCase = new UseCase(Products, this);
            Request = new CreateProductRequest(ProductType.Id.Value, "Product", "Standard");
        }

        public ProductType? ProductType { get; set; }
        public ProductRepositoryFake Products { get; }
        public UseCase UseCase { get; }
        public CreateProductRequest Request { get; }

        public Task<ProductType?> GetByIdAsync(
            ProductTypeId id,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductType);
        }
    }

    private sealed class ProductRepositoryFake : IProductRepository
    {
        public Product? AddedProduct { get; private set; }
        public ProductConcurrencyToken? ReturnedToken { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(
            ProductId id,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ProductConcurrencyToken> AddAsync(
            Product product,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddedProduct = product;
            ReturnedToken = ProductConcurrencyToken.Create(product.Id, Guid.NewGuid());
            return Task.FromResult(ReturnedToken);
        }

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
