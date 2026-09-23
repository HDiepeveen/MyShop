using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductAttributeValue.GetProductAttributeValue;

namespace MyShop.Application.Tests.Catalog.GetProductAttributeValue;

public sealed class GetProductAttributeValueTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task ExecuteAsync_ReturnsStoredValueAndRevisionWithoutWriting(int kind)
    {
        var repository = new RepositoryFake();
        var value = Value(kind, repository.DefinitionId);
        repository.Product.SetAttributeValue(value);
        repository.Product.SetVariantAttributeValue(repository.Product.Variants.Single().Id,
            TextAttributeValue.Create(repository.DefinitionId, "Different variant value"));
        using var source = new CancellationTokenSource();

        var result = await new UseCase(repository).ExecuteAsync(repository.Query, source.Token);

        Assert.True(result.IsSuccess);
        Assert.Same(value, result.Snapshot!.Value);
        Assert.Equal(repository.Token.Revision, result.Snapshot.Revision);
        Assert.Equal(source.Token, repository.ReadCancellation);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ReturnsSpecificFailureWithoutFallbackToVariant(bool missingProduct)
    {
        var repository = new RepositoryFake { Missing = missingProduct };
        repository.Product.SetVariantAttributeValue(repository.Product.Variants.Single().Id,
            TextAttributeValue.Create(repository.DefinitionId, "Variant only"));
        var result = await new UseCase(repository).ExecuteAsync(repository.Query, CancellationToken.None);
        Assert.Equal(missingProduct ? GetProductAttributeValueFailure.ProductNotFound :
            GetProductAttributeValueFailure.AttributeValueNotFound, result.Failure);
        Assert.Null(result.Snapshot);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_RejectsEmptyIdsBeforeRead(bool product)
    {
        var repository = new RepositoryFake();
        await Assert.ThrowsAsync<ArgumentException>(() => new UseCase(repository).ExecuteAsync(
            product ? repository.Query with { ProductId = default } :
                repository.Query with { AttributeDefinitionId = default }, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsCanceledAndNullQueryBeforeRead()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UseCase(repository).ExecuteAsync(repository.Query, source.Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new UseCase(repository).ExecuteAsync(null!, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() => Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    private static AttributeValue Value(int kind, AttributeDefinitionId id) => kind switch
    {
        0 => TextAttributeValue.Create(id, " Red "),
        1 => IntegerAttributeValue.Create(id, long.MaxValue),
        2 => DecimalAttributeValue.Create(id, 1.2345678901234567890123456789m),
        3 => BooleanAttributeValue.Create(id, false),
        4 => DateAttributeValue.Create(id, new DateOnly(2026, 1, 1)),
        5 => ChoiceAttributeValue.Create(id, ChoiceValue.Create("Red")),
        _ => MultiChoiceAttributeValue.Create(id, [ChoiceValue.Create("Red"), ChoiceValue.Create("Blue")])
    };

    private sealed class RepositoryFake : IProductRepository
    {
        public RepositoryFake()
        {
            Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
            Query = new(Product.Id, DefinitionId);
        }
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "First");
        public AttributeDefinitionId DefinitionId { get; } = AttributeDefinitionId.New();
        public ProductConcurrencyToken Token { get; }
        public GetProductAttributeValueQuery Query { get; }
        public bool Missing { get; init; }
        public int ReadCalls { get; private set; }
        public CancellationToken ReadCancellation { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Assert.Equal(Product.Id, id);
            cancellationToken.ThrowIfCancellationRequested();
            ReadCalls++;
            ReadCancellation = cancellationToken;
            return Task.FromResult(Missing ? null : new ProductSnapshot(Product, Token));
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException("Read-only");
    }
}
