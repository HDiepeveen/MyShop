using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RenameProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductTypeAttribute.RenameProductTypeAttribute;

namespace MyShop.Application.Tests.Catalog.RenameProductTypeAttribute;

public sealed class RenameProductTypeAttributeTests
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
        var command = new RenameProductTypeAttributeCommand(
            ProductTypeId.New(), AttributeDefinitionId.New(), "Size");
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new UseCase(store, store).ExecuteAsync(command, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullCommandAndEmptyIds()
    {
        var store = new StoreFake(null);
        var useCase = new UseCase(store, store);
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new(default, AttributeDefinitionId.New(), "Size"), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new(ProductTypeId.New(), default, "Size"), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_RenamesExistingAttribute()
    {
        var store = new StoreFake(ProductType.Create("Type"));
        var attribute = store.ProductType!.AddAttribute(AttributeDefinitionId.New(),
            AttributeCode.Create("size"), "Old", AttributeDataType.Text, false, false, AttributeScope.Product);
        var result = await new UseCase(store, store).ExecuteAsync(
            new(store.ProductType.Id, attribute.Id, "Size"), CancellationToken.None);
        Assert.Equal(ProductTypeAttributeUpdateResult.Succeeded, result);
        Assert.Equal("Size", attribute.DisplayName);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task ExecuteAsync_MissingAttribute_ReturnsNotFound()
    {
        var store = new StoreFake(ProductType.Create("Type"));
        var result = await new UseCase(store, store).ExecuteAsync(
            new(store.ProductType!.Id, AttributeDefinitionId.New(), "Size"), CancellationToken.None);
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
