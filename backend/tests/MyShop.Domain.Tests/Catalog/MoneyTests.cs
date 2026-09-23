using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class MoneyTests
{
    [Fact] public void Create_NormalizesCurrency() => Assert.Equal("EUR", Money.Create(12.345m, " eur ").Currency);
    [Fact] public void Create_RoundsToCents() => Assert.Equal(12.34m, Money.Create(12.345m, "EUR").Amount);
    [Fact] public void Create_AllowsZero() => Assert.Equal(0m, Money.Create(0, "EUR").Amount);
    [Fact] public void Create_RejectsNegativeAmount() => Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(-.01m, "EUR"));
    [Fact] public void Create_RejectsInvalidCurrency() => Assert.Throws<ArgumentException>(() => Money.Create(1, "EURO"));
    [Fact] public void VariantPrice_CanBeSetAndCleared()
    {
        var product = Product.Create("Demo", ProductTypeId.New(), "Default");
        var variant = Assert.Single(product.Variants);
        product.SetVariantPrice(variant.Id, Money.Create(9.99m, "EUR"));
        Assert.Equal(Money.Create(9.99m, "EUR"), variant.Price);
        product.ClearVariantPrice(variant.Id);
        Assert.Null(variant.Price);
    }
    [Fact]
    public void SetVariantPrice_RejectsDefaultWithoutChangingExistingPrice()
    {
        var product = Product.Create("Demo", ProductTypeId.New(), "Default");
        var variant = Assert.Single(product.Variants);
        product.SetVariantPrice(variant.Id, Money.Create(25m, "EUR"));
        Assert.Throws<ArgumentException>(() => product.SetVariantPrice(variant.Id, default));
        Assert.Equal(Money.Create(25m, "EUR"), variant.Price);
    }

    [Fact]
    public void Rehydrate_RejectsPresentDefaultPrice() =>
        Assert.Throws<ArgumentException>(() => ProductVariant.Rehydrate(
            ProductVariantId.New(), "Default", null, [], default(Money)));

    [Fact]
    public void Rehydrate_AllowsAbsentAndZeroPrices()
    {
        Assert.Null(ProductVariant.Rehydrate(ProductVariantId.New(), "Default", null, []).Price);
        Assert.Equal(Money.Create(0m, "EUR"), ProductVariant.Rehydrate(
            ProductVariantId.New(), "Default", null, [], Money.Create(0m, "EUR")).Price);
    }
    [Fact]
    public void Create_RejectsAmountBeyondSupportedPrecision() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(10000000000000000m, "EUR"));

    [Fact]
    public void Create_AllowsLargestSupportedAmount() =>
        Assert.Equal(9999999999999999.99m, Money.Create(9999999999999999.99m, "EUR").Amount);

}
