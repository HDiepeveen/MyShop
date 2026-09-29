using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductAttributeValidation.GetProductAttributeValidation;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class GetProductAttributeValidationEndpointTests
{
    [Theory]
    [InlineData(AttributeScope.Product)]
    [InlineData(AttributeScope.Variant)]
    public async Task MapsIssueIdentityScopeAndCode(AttributeScope scope)
    {
        var store = new Store();
        var attribute = store.Type!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("required"), "Required",
            AttributeDataType.Text, true, false, scope);
        var result = await GetProductAttributeValidationEndpoint.ExecuteAsync(store.Product.Id.Value, new UseCase(store, store), default);
        var response = Assert.IsType<Ok<ProductAttributeValidationResponse>>(result.Result).Value!;
        Assert.Equal(store.Product.Id.Value, response.ProductId);
        Assert.False(response.IsValid);
        var issue = Assert.Single(response.Issues);
        Assert.Equal(attribute.Id.Value, issue.AttributeDefinitionId);
        Assert.Equal(scope == AttributeScope.Product ? (Guid?)null : store.Product.Variants.Single().Id.Value, issue.VariantId);
        Assert.Equal("MissingRequired", issue.Code);
    }

    [Fact]
    public async Task CompleteProductReturnsValidWithEmptyIssues()
    {
        var store = new Store();
        var result = await GetProductAttributeValidationEndpoint.ExecuteAsync(store.Product.Id.Value, new UseCase(store, store), default);
        var response = Assert.IsType<Ok<ProductAttributeValidationResponse>>(result.Result).Value!;
        Assert.True(response.IsValid);
        Assert.Empty(response.Issues);
    }


    [Fact]
    public async Task MissingProductAndTypeHaveDistinctNotFoundResponses()
    {
        var store = new Store { MissingProduct = true };
        var result = await GetProductAttributeValidationEndpoint.ExecuteAsync(store.Product.Id.Value, new UseCase(store, store), default);
        Assert.Equal("Product not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
        Assert.Equal(1, store.Reads);
        store.MissingProduct = false;
        store.Type = null;
        result = await GetProductAttributeValidationEndpoint.ExecuteAsync(store.Product.Id.Value, new UseCase(store, store), default);
        Assert.Equal("Product type not found", Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Fact]
    public async Task InvalidIdAndPreCancelledQueryDoNotReadStorage()
    {
        var store = new Store();
        var result = await GetProductAttributeValidationEndpoint.ExecuteAsync(Guid.Empty, new UseCase(store, store), default);
        Assert.Equal("Invalid product ID", Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetProductAttributeValidationEndpoint.ExecuteAsync(
            store.Product.Id.Value, new UseCase(store, store), new CancellationToken(true)));
        Assert.Equal(0, store.Reads);
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetProductAttributeValidationEndpoint.ExecuteAsync(Guid.NewGuid(), null!, default));
    }

    [Fact]
    public async Task StorageArgumentFailureIsNotMisreportedAsBadRequest()
    {
        var expected = new ArgumentException("storage configuration");
        var store = new Store { Error = expected };
        var actual = await Assert.ThrowsAsync<ArgumentException>(() => GetProductAttributeValidationEndpoint.ExecuteAsync(
            store.Product.Id.Value, new UseCase(store, store), default));
        Assert.Same(expected, actual);
    }

    internal sealed class Store : IProductRepository, IProductTypeRepository
    {
        public Store() => Product = Product.Create("Product", Type!.Id, "First");
        public Product Product { get; }
        public ProductType? Type { get; set; } = ProductType.Create("Type");
        public bool MissingProduct { get; set; }
        public Exception? Error { get; set; }
        public int Reads { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Reads++;
            if (Error is not null) throw Error;
            return Task.FromResult(MissingProduct || id != Product.Id ? null : new ProductSnapshot(Product, ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid())));
        }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            Reads++;
            Assert.Equal(Product.ProductTypeId, id);
            return Task.FromResult(Type);
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
