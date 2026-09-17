using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AssignProductToCategory.AssignProductToCategory;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class AssignProductToCategoryEndpointTests
{
    [Fact]
    public void MapAssignProductToCategory_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => AssignProductToCategoryEndpoint.MapAssignProductToCategory(null!));

    [Fact]
    public void MapAssignProductToCategory_MapsNamedPutRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapAssignProductToCategory());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal(
            "/api/products/{productId:guid}/categories/{categoryId:guid}",
            endpoint.RoutePattern.RawText);
        Assert.Equal("AssignProductToCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["PUT"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_AssignsCategoryAndReturnsNoContent()
    {
        var scenario = new Scenario();

        var result = await scenario.ExecuteAsync();

        Assert.IsType<NoContent>(result.Result);
        Assert.Contains(scenario.Category.Id, scenario.Product.CategoryIds);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Equal(1, scenario.Categories.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyAssigned_IsIdempotent()
    {
        var scenario = new Scenario();
        scenario.Product.AssignToCategory(scenario.Category.Id);

        var result = await scenario.ExecuteAsync();

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WhenProductOrCategoryDoesNotExist_ReturnsSpecificNotFound(
        bool productIsMissing)
    {
        var scenario = new Scenario
        {
            ProductIsMissing = productIsMissing,
            CategoryIsMissing = !productIsMissing
        };

        var result = await scenario.ExecuteAsync();

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal(
            productIsMissing ? "Product not found" : "Category not found",
            notFound.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExecuteAsync_WhenIdIsInvalid_ReturnsBadRequest(
        bool emptyProductId,
        bool emptyCategoryId)
    {
        var scenario = new Scenario();

        var result = await AssignProductToCategoryEndpoint.ExecuteAsync(
            emptyProductId ? Guid.Empty : scenario.Product.Id.Value,
            emptyCategoryId ? Guid.Empty : scenario.Category.Id.Value,
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product category assignment", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductChanged_ReturnsConflict()
    {
        var scenario = new Scenario { HasConcurrencyConflict = true };

        var result = await scenario.ExecuteAsync();

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
            scenario.ExecuteAsync(source.Token));

        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Product.Create("Shirt", ProductTypeId.New(), "Medium");
            Category = Category.CreateRoot("Clothing");
            Products = new ProductRepositoryFake(this);
            Categories = new CategoryRepositoryFake(this);
            UseCase = new UseCase(Products, Categories);
        }

        public Product Product { get; }
        public Category Category { get; }
        public bool ProductIsMissing { get; set; }
        public bool CategoryIsMissing { get; set; }
        public bool HasConcurrencyConflict { get; set; }
        public ProductRepositoryFake Products { get; }
        public CategoryRepositoryFake Categories { get; }
        public UseCase UseCase { get; }

        public Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>>
            ExecuteAsync(CancellationToken cancellationToken = default) =>
            AssignProductToCategoryEndpoint.ExecuteAsync(
                Product.Id.Value,
                Category.Id.Value,
                UseCase,
                cancellationToken);
    }

    private sealed class ProductRepositoryFake(Scenario scenario) : IProductRepository
    {
        private readonly ProductConcurrencyToken _readToken =
            ProductConcurrencyToken.Create(scenario.Product.Id, Guid.NewGuid());

        public int SaveCalls { get; private set; }

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
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

    private sealed class CategoryRepositoryFake(Scenario scenario) : ICategoryRepository
    {
        public int GetCalls { get; private set; }

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.CategoryIsMissing ? null : scenario.Category);
        }
    }
}
