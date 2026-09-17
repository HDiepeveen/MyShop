using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.AddProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AddProductTypeAttribute.AddProductTypeAttribute;

namespace MyShop.Application.Tests.Catalog.AddProductTypeAttribute;

public sealed class AddProductTypeAttributeTests
{
    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var store = new StoreFake();
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_StopsBeforeRepositoryAccess()
    {
        var store = new StoreFake();
        var command = new AddProductTypeAttributeCommand(ProductTypeId.New(), "size", "Size",
            AttributeDataType.Text, false, false, AttributeScope.Product);
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new UseCase(store, store).ExecuteAsync(command, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullCommandAndEmptyProductTypeId()
    {
        var store = new StoreFake();
        var useCase = new UseCase(store, store);
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(
            default, "size", "Size", AttributeDataType.Text, false, false, AttributeScope.Product),
            CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_AddsAndPersistsAttribute()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Clothing") };
        var attribute = await new UseCase(store, store).ExecuteAsync(new(
            store.ProductType.Id, "colour", "Colour", AttributeDataType.Choice,
            true, true, AttributeScope.Variant), CancellationToken.None);

        Assert.NotNull(attribute);
        Assert.Equal("colour", attribute.Code.Value);
        Assert.Same(attribute, Assert.Single(store.ProductType.AttributeDefinitions));
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_MissingProductType_ReturnsNull()
    {
        var store = new StoreFake();
        var result = await new UseCase(store, store).ExecuteAsync(new(
            ProductTypeId.New(), "colour", "Colour", AttributeDataType.Choice,
            false, true, AttributeScope.Product), CancellationToken.None);
        Assert.Null(result);
        Assert.Equal(0, store.SaveCount);
    }

    private sealed class StoreFake : IProductTypeRepository, IProductTypeWriter
    {
        public ProductType? ProductType { get; set; }
        public int SaveCount { get; private set; }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken token) =>
            Task.FromResult(ProductType);
        public Task AddAsync(ProductType productType, CancellationToken token) => throw new NotSupportedException();
        public Task SaveAsync(ProductType productType, CancellationToken token)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
