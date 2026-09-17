using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RenameProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductTypeAttribute.RenameProductTypeAttribute;

namespace MyShop.Application.Tests.Catalog.RenameProductTypeAttribute;

public sealed class RenameProductTypeAttributeTests
{
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
