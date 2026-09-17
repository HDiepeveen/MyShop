using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.DeleteProductType;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.DeleteProductType.DeleteProductType;

namespace MyShop.Application.Tests.Catalog.DeleteProductType;

public sealed class DeleteProductTypeTests
{
    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var store = new StoreFake();
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, store, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_StopsBeforeRepositoryAccess()
    {
        var store = new StoreFake();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new UseCase(store, store, store).ExecuteAsync(
                ProductTypeId.New(), new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task ExecuteAsync_UnusedProductType_IsDeleted()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Old") };
        var result = await new UseCase(store, store, store).ExecuteAsync(
            store.ProductType.Id, CancellationToken.None);
        Assert.Equal(DeleteProductTypeOutcome.Succeeded, result.Outcome);
        Assert.Equal(store.ProductType.Id, store.DeletedId);
    }

    [Fact]
    public async Task ExecuteAsync_UsedProductType_ReturnsConflictWithoutDelete()
    {
        var store = new StoreFake { ProductType = ProductType.Create("Used"), ProductCount = 3 };
        var result = await new UseCase(store, store, store).ExecuteAsync(
            store.ProductType.Id, CancellationToken.None);
        Assert.Equal(DeleteProductTypeOutcome.InUse, result.Outcome);
        Assert.Equal(3, result.ProductCount);
        Assert.Null(store.DeletedId);
    }

    [Fact]
    public async Task ExecuteAsync_MissingProductType_SkipsUsageAndDelete()
    {
        var store = new StoreFake { ProductCount = 3 };
        var result = await new UseCase(store, store, store).ExecuteAsync(
            ProductTypeId.New(), CancellationToken.None);
        Assert.Equal(DeleteProductTypeOutcome.NotFound, result.Outcome);
        Assert.Equal(0, store.UsageCalls);
    }

    private sealed class StoreFake
        : IProductTypeRepository, IProductTypeUsageRepository, IProductTypeDeleter
    {
        public ProductType? ProductType { get; set; }
        public int ProductCount { get; set; }
        public int UsageCalls { get; private set; }
        public ProductTypeId? DeletedId { get; private set; }
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken token) =>
            Task.FromResult(ProductType);
        public Task<int> CountProductsAsync(ProductTypeId id, CancellationToken token)
        {
            UsageCalls++;
            return Task.FromResult(ProductCount);
        }
        public Task DeleteAsync(ProductTypeId id, CancellationToken token)
        {
            DeletedId = id;
            return Task.CompletedTask;
        }
    }
}
