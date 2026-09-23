using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductAttributeValue.GetProductAttributeValue;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class GetProductAttributeValueEndpointTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task ExecuteAsync_MapsAllStoredTypesWithoutLosingValues(int kind)
    {
        var repository = new RepositoryFake();
        var value = Value(kind, repository.DefinitionId);
        repository.Product.SetAttributeValue(value);
        var result = await GetProductAttributeValueEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.DefinitionId.Value, new UseCase(repository), CancellationToken.None);
        var response = Assert.IsType<Ok<AttributeValueDetailsResponse>>(result.Result).Value!;
        Assert.Equal(repository.Token.Revision, response.Revision);
        Assert.Equal(repository.DefinitionId.Value, response.Attribute.AttributeDefinitionId);
        Assert.Equal(value.DataType.ToString(), response.Attribute.DataType);
        switch (kind)
        {
            case 0: Assert.Equal(" Red ", Assert.IsType<string>(response.Attribute.Value)); break;
            case 1: Assert.Equal(long.MaxValue, Assert.IsType<long>(response.Attribute.Value)); break;
            case 2: Assert.Equal(1.2345678901234567890123456789m, Assert.IsType<decimal>(response.Attribute.Value)); break;
            case 3: Assert.False(Assert.IsType<bool>(response.Attribute.Value)); break;
            case 4: Assert.Equal(new DateOnly(2026, 1, 1), Assert.IsType<DateOnly>(response.Attribute.Value)); break;
            case 5: Assert.Equal("Red", Assert.IsType<string>(response.Attribute.Value)); break;
            default: Assert.Equal(new[] { "Red", "Blue" }, Assert.IsType<string[]>(response.Attribute.Value)); break;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ReturnsSpecificNotFound(bool missingProduct)
    {
        var repository = new RepositoryFake { Missing = missingProduct };
        var result = await GetProductAttributeValueEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.DefinitionId.Value, new UseCase(repository), CancellationToken.None);
        Assert.Equal(missingProduct ? "Product not found" : "Attribute value not found",
            Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_EmptyIdReturnsBadRequestBeforeRead(bool product)
    {
        var repository = new RepositoryFake();
        var result = await GetProductAttributeValueEndpoint.ExecuteAsync(
            product ? Guid.Empty : repository.Product.Id.Value,
            product ? repository.DefinitionId.Value : Guid.Empty, new UseCase(repository), CancellationToken.None);
        Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsCanceledTokenAndNullDependency()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetProductAttributeValueEndpoint.ExecuteAsync(
            repository.Product.Id.Value, repository.DefinitionId.Value, new UseCase(repository), source.Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetProductAttributeValueEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public void Map_RejectsNullBuilder() => Assert.Throws<ArgumentNullException>(() =>
        GetProductAttributeValueEndpoint.MapGetProductAttributeValue(null!));

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
        }
        public Product Product { get; } = Product.Create("Product", ProductTypeId.New(), "First");
        public AttributeDefinitionId DefinitionId { get; } = AttributeDefinitionId.New();
        public ProductConcurrencyToken Token { get; }
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
