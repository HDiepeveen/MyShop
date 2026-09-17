using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RenameProductType;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductType.RenameProductType;

namespace MyShop.Application.Tests.Catalog.RenameProductType;

public sealed class RenameProductTypeTests
{
    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var store = new StoreFake();
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!));
    }

    [Fact]
    public async Task ExecuteAsync_RenamesAndPersistsExistingProductType()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Old") };
        var result = await new UseCase(store, store).ExecuteAsync(
            new RenameProductTypeCommand(store.ProductType.Id, "New"), CancellationToken.None);

        Assert.True(result);
        Assert.Equal("New", store.ProductType.Name);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_MissingProductType_ReturnsFalse()
    {
        var store = new StoreFake();
        Assert.False(await new UseCase(store, store).ExecuteAsync(
            new RenameProductTypeCommand(ProductTypeId.New(), "New"), CancellationToken.None));
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_UnchangedName_DoesNotWrite()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Same") };
        Assert.True(await new UseCase(store, store).ExecuteAsync(
            new RenameProductTypeCommand(store.ProductType.Id, "Same"), CancellationToken.None));
        Assert.Equal(0, store.SaveCount);
    }

    private sealed class StoreFake : IProductTypeRepository, IProductTypeWriter
    {
        public ProductType? ProductType { get; set; }
        public int SaveCount { get; private set; }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken) =>
            Task.FromResult(ProductType);
        public Task AddAsync(ProductType productType, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task SaveAsync(ProductType productType, CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
