using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductVariantStockTests
{
    [Fact]
    public void UntrackedStockIsUnlimitedAndTrackedStockCannotOversell()
    {
        var product = Product.Create("Shirt", ProductTypeId.New(), "Small");
        var variant = product.Variants.Single();
        Assert.Null(variant.StockQuantity);
        Assert.True(variant.CanFulfill(99));

        product.SetVariantStockQuantity(variant.Id, 2);
        Assert.True(variant.CanFulfill(2));
        Assert.False(variant.CanFulfill(3));
        product.ReserveVariantStock(variant.Id, 2);
        Assert.Equal(0, variant.StockQuantity);
        Assert.Throws<InvalidOperationException>(() => product.ReserveVariantStock(variant.Id, 1));
        product.ReleaseVariantStock(variant.Id, 1);
        Assert.Equal(1, variant.StockQuantity);
        product.ClearVariantStockTracking(variant.Id);
        Assert.Null(variant.StockQuantity);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void RejectsNegativeStock(int quantity)
    {
        var product = Product.Create("Shirt", ProductTypeId.New(), "Small");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.SetVariantStockQuantity(product.Variants.Single().Id, quantity));
    }
}
