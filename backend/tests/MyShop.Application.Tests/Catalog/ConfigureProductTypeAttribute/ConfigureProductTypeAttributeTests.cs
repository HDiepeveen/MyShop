using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ConfigureProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ConfigureProductTypeAttribute.ConfigureProductTypeAttribute;

namespace MyShop.Application.Tests.Catalog.ConfigureProductTypeAttribute;

public sealed class ConfigureProductTypeAttributeTests
{
    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var store = new StoreFake(null);
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!));
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesBothFlagsAndPersistsOnce()
    {
        var store = new StoreFake(ProductType.Create("Type"));
        var attribute = store.ProductType!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("size"),
            "Size", AttributeDataType.Text, false, false, AttributeScope.Product);
        var result = await new UseCase(store, store).ExecuteAsync(
            new(store.ProductType.Id, attribute.Id, true, true), CancellationToken.None);
        Assert.Equal(ProductTypeAttributeUpdateResult.Succeeded, result);
        Assert.True(attribute.IsRequired);
        Assert.True(attribute.IsFilterable);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task ExecuteAsync_UnchangedFlags_DoesNotPersist()
    {
        var store = new StoreFake(ProductType.Create("Type"));
        var attribute = store.ProductType!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("size"),
            "Size", AttributeDataType.Text, false, true, AttributeScope.Product);
        var result = await new UseCase(store, store).ExecuteAsync(
            new(store.ProductType.Id, attribute.Id, false, true), CancellationToken.None);
        Assert.Equal(ProductTypeAttributeUpdateResult.Succeeded, result);
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
