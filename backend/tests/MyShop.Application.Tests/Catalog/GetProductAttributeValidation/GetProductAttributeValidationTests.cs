using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductAttributeValidation;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductAttributeValidation.GetProductAttributeValidation;

namespace MyShop.Application.Tests.Catalog.GetProductAttributeValidation;

public sealed class GetProductAttributeValidationTests
{
    [Fact]
    public async Task ReturnsIssuesAndForwardsBothIdsAndCancellationWithoutWriting()
    {
        var store = new Store();
        var definition = store.Type!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("required"), "Required",
            AttributeDataType.Text, true, false, AttributeScope.Product);
        using var source = new CancellationTokenSource();
        var result = await new UseCase(store, store).ExecuteAsync(new(store.Product.Id), source.Token);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Equal(definition.Id, Assert.Single(result.Issues!).AttributeDefinitionId);
        Assert.Equal(store.Product.Id, store.ProductId);
        Assert.Equal(store.Type.Id, store.TypeId);
        Assert.Equal(source.Token, store.ProductToken);
        Assert.Equal(source.Token, store.TypeToken);
        Assert.Equal(1, store.ProductReads);
        Assert.Equal(1, store.TypeReads);
    }

    [Fact]
    public async Task MissingProductDoesNotReadType()
    {
        var store = new Store { MissingProduct = true };
        var result = await new UseCase(store, store).ExecuteAsync(new(store.Product.Id), default);
        Assert.Equal(GetProductAttributeValidationFailure.ProductNotFound, result.Failure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Issues);
        Assert.Equal(0, store.TypeReads);
    }

    [Fact]
    public async Task MissingTypeIsDistinctFromValidProduct()
    {
        var store = new Store { Type = null };
        var result = await new UseCase(store, store).ExecuteAsync(new(store.Product.Id), default);
        Assert.Equal(GetProductAttributeValidationFailure.ProductTypeNotFound, result.Failure);
        Assert.Null(result.Issues);
    }

    [Fact]
    public async Task ValidProductReturnsEmptyIssues()
    {
        var store = new Store();
        var result = await new UseCase(store, store).ExecuteAsync(new(store.Product.Id), default);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Issues!);
    }

    [Fact]
    public async Task RejectsInvalidInputAndPreCancellationBeforeReading()
    {
        var store = new Store();
        var useCase = new UseCase(store, store);
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, default));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(default), default));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(new(store.Product.Id), new CancellationToken(true)));
        Assert.Equal(0, store.ProductReads);
        Assert.Equal(0, store.TypeReads);
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!));
        Assert.Throws<ArgumentNullException>(() => GetProductAttributeValidationResult.Succeeded(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StorageFailuresPropagate(bool typeRead)
    {
        var error = new InvalidOperationException("storage");
        var store = new Store { ProductError = typeRead ? null : error, TypeError = typeRead ? error : null };
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => new UseCase(store, store).ExecuteAsync(new(store.Product.Id), default));
        Assert.Same(error, actual);
    }


    [Fact]
    public void SuccessResultCopiesCallerOwnedIssueCollection()
    {
        var issue = new ProductAttributeIssue(AttributeDefinitionId.New(), null, ProductAttributeIssueCode.MissingRequired);
        var supplied = new List<ProductAttributeIssue> { issue };
        var result = GetProductAttributeValidationResult.Succeeded(supplied);
        supplied.Clear();
        Assert.Equal(issue, Assert.Single(result.Issues!));
        Assert.Throws<NotSupportedException>(() => ((IList<ProductAttributeIssue>)result.Issues!).Clear());
    }

    private sealed class Store : IProductRepository, IProductTypeRepository
    {
        public Store() => Product = Product.Create("Product", Type!.Id, "First");
        public ProductType? Type { get; init; } = ProductType.Create("Type");
        public Product Product { get; }
        public bool MissingProduct { get; init; }
        public Exception? ProductError { get; init; }
        public Exception? TypeError { get; init; }
        public int ProductReads { get; private set; }
        public int TypeReads { get; private set; }
        public ProductId ProductId { get; private set; }
        public ProductTypeId TypeId { get; private set; }
        public CancellationToken ProductToken { get; private set; }
        public CancellationToken TypeToken { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            ProductReads++;
            ProductId = id;
            ProductToken = cancellationToken;
            if (ProductError is not null) throw ProductError;
            return Task.FromResult(MissingProduct ? null : new ProductSnapshot(Product, ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid())));
        }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            TypeReads++;
            TypeId = id;
            TypeToken = cancellationToken;
            if (TypeError is not null) throw TypeError;
            return Task.FromResult(Type);
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException("Query must not write.");
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken token, CancellationToken cancellationToken) => throw new NotSupportedException("Query must not write.");
    }
}
