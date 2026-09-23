using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetVariantAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetVariantAttributeValue.GetVariantAttributeValue;

namespace MyShop.Application.Tests.Catalog.GetVariantAttributeValue;

public sealed class GetVariantAttributeValueTests
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
        repository.Product.SetVariantAttributeValue(repository.Variant.Id, value);
        repository.Product.SetAttributeValue(TextAttributeValue.Create(repository.DefinitionId, "Different product value"));
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
    public async Task ExecuteAsync_ReturnsSpecificFailureWithoutFallbackToProduct(bool missingProduct)
    {
        var repository = new RepositoryFake { Missing = missingProduct };
        repository.Product.SetAttributeValue(TextAttributeValue.Create(repository.DefinitionId, "Product only"));
        var result = await new UseCase(repository).ExecuteAsync(repository.Query, CancellationToken.None);
        Assert.Equal(missingProduct ? GetVariantAttributeValueFailure.ProductNotFound :
            GetVariantAttributeValueFailure.AttributeValueNotFound, result.Failure);
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
    public async Task ExecuteAsync_RejectsEmptyVariantIdBeforeRead()
    {
        var repository = new RepositoryFake();
        await Assert.ThrowsAsync<ArgumentException>(() => new UseCase(repository).ExecuteAsync(
            repository.Query with { ProductVariantId = default }, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsMissingVariantAndDoesNotReadOtherVariantValue()
    {
        var repository = new RepositoryFake();
        var other = repository.Product.AddVariant("Other");
        repository.Product.SetVariantAttributeValue(other.Id, TextAttributeValue.Create(repository.DefinitionId, "Other value"));
        var result = await new UseCase(repository).ExecuteAsync(repository.Query, CancellationToken.None);
        Assert.Equal(GetVariantAttributeValueFailure.AttributeValueNotFound, result.Failure);
        var missing = await new UseCase(repository).ExecuteAsync(
            repository.Query with { ProductVariantId = ProductVariantId.New() }, CancellationToken.None);
        Assert.Equal(GetVariantAttributeValueFailure.VariantNotFound, missing.Failure);
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
            Query = new(Product.Id, Variant.Id, DefinitionId);
        }
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "First");
        public ProductVariant Variant => Product.Variants.First();
        public AttributeDefinitionId DefinitionId { get; } = AttributeDefinitionId.New();
        public ProductConcurrencyToken Token { get; }
        public GetVariantAttributeValueQuery Query { get; }
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
