using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RemoveProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductTypeAttribute.RemoveProductTypeAttribute;

namespace MyShop.Application.Tests.Catalog.RemoveProductTypeAttribute;

public sealed class RemoveProductTypeAttributeTests
{
    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var store = new StoreFake(null);
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_StopsBeforeRepositoryAccess()
    {
        var store = new StoreFake(null);
        var command = new RemoveProductTypeAttributeCommand(ProductTypeId.New(), AttributeDefinitionId.New());
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new UseCase(store, store).ExecuteAsync(command, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task ExecuteAsync_RemovesExistingAttributeAndPersists()
    {
        var store = new StoreFake(ProductType.Create("Type"));
        var attribute = store.ProductType!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("size"),
            "Size", AttributeDataType.Text, false, false, AttributeScope.Product);
        var result = await new UseCase(store, store).ExecuteAsync(
            new(store.ProductType.Id, attribute.Id), CancellationToken.None);
        Assert.Equal(ProductTypeAttributeUpdateResult.Succeeded, result);
        Assert.Empty(store.ProductType.AttributeDefinitions);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task ExecuteAsync_MissingAttribute_DoesNotPersist()
    {
        var store = new StoreFake(ProductType.Create("Type"));
        var result = await new UseCase(store, store).ExecuteAsync(
            new(store.ProductType!.Id, AttributeDefinitionId.New()), CancellationToken.None);
        Assert.Equal(ProductTypeAttributeUpdateResult.AttributeNotFound, result);
        Assert.Equal(0, store.Saves);
    }

    private sealed class StoreFake(ProductType? productType) : IProductTypeRepository, IProductTypeWriter
    {
        public ProductType? ProductType { get; } = productType;
        public int Saves { get; private set; }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken token) => Task.FromResult(ProductType);
        public Task AddAsync(ProductType productType, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(ProductType productType, CancellationToken token) { Saves++; return Task.CompletedTask; }
    }
}
