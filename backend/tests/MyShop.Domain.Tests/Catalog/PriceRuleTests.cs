using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class PriceRuleTests
{
    private static readonly DateTimeOffset At = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact] public void PercentageDiscount_AppliesToBasePrice() => Assert.Equal(Money.Create(80, "EUR"), Rule(20).Apply(Money.Create(100, "EUR")));
    [Fact] public void FixedDiscount_AppliesToBasePrice() => Assert.Equal(Money.Create(90, "EUR"), PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 10, 1).Apply(Money.Create(100, "EUR")));
    [Fact] public void Discount_DoesNotProduceNegativePrice() => Assert.Equal(Money.Create(0, "EUR"), PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 200, 1).Apply(Money.Create(100, "EUR")));
    [Fact] public void Rule_IsActiveInsidePeriod() => Assert.True(Rule(20, At.AddDays(-1), At.AddDays(1)).IsActiveAt(At));
    [Fact] public void Rule_IsInactiveOutsidePeriod() => Assert.False(Rule(20, At.AddDays(1), null).IsActiveAt(At));
    [Fact] public void Create_RejectsPercentageAboveHundred() => Assert.Throws<ArgumentOutOfRangeException>(() => PriceRule.Create("x", PriceAdjustmentType.PercentageDiscount, 101, 1));
    [Fact] public void Create_RejectsReversedPeriod() => Assert.Throws<ArgumentException>(() => PriceRule.Create("x", PriceAdjustmentType.FixedDiscount, 1, 1, At, At.AddSeconds(-1)));
    [Fact] public void Create_TrimsName() => Assert.Equal("Sale", PriceRule.Create(" Sale ", PriceAdjustmentType.FixedDiscount, 1, 1).Name);
    [Fact] public void Variant_CalculatesOnlyHighestPriorityActiveRule()
    {
        var product = Product.Create("Demo", ProductTypeId.New(), "Default");
        var variant = Assert.Single(product.Variants);
        product.SetVariantPrice(variant.Id, Money.Create(100, "EUR"));
        product.AddVariantPriceRule(variant.Id, PriceRule.Create("Low", PriceAdjustmentType.PercentageDiscount, 10, 1));
        product.AddVariantPriceRule(variant.Id, PriceRule.Create("High", PriceAdjustmentType.PercentageDiscount, 25, 2));
        Assert.Equal(Money.Create(75, "EUR"), variant.CalculatePrice(At));
    }
    [Fact] public void Variant_UsesBasePriceWhenNoRuleIsActive()
    {
        var product = Product.Create("Demo", ProductTypeId.New(), "Default");
        var variant = Assert.Single(product.Variants);
        product.SetVariantPrice(variant.Id, Money.Create(100, "EUR"));
        product.AddVariantPriceRule(variant.Id, PriceRule.Create("Future", PriceAdjustmentType.FixedDiscount, 10, 1, At.AddDays(1)));
        Assert.Equal(Money.Create(100, "EUR"), variant.CalculatePrice(At));
    }
    [Theory]
    [InlineData(PriceAdjustmentType.FixedDiscount)]
    [InlineData(PriceAdjustmentType.PercentageDiscount)]
    public void Create_RejectsValueThatRoundsToZero(PriceAdjustmentType type) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceRule.Create("Sale", type, 0.004m, 1));

    [Fact]
    public void Create_SmallestPositiveRoundedDiscountCanBeRehydrated()
    {
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 0.006m, 1);
        var restored = PriceRule.Rehydrate(rule.Id, rule.Name, rule.AdjustmentType,
            rule.Value, rule.Priority, rule.StartsAt, rule.EndsAt);
        Assert.Equal(0.01m, restored.Value);
        Assert.Equal(rule.Id, restored.Id);
    }

    [Fact]
    public void Create_RejectsOverlongName() =>
        Assert.Throws<ArgumentException>(() => PriceRule.Create(new string('x', 201), PriceAdjustmentType.FixedDiscount, 1m, 0));

    [Fact]
    public void Create_RejectsValueBeyondSupportedPrecision() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 10000000000000000m, 0));

    [Fact]
    public void Create_AllowsLargestSupportedNameAndFixedDiscount()
    {
        var rule = PriceRule.Create(" " + new string('x', 200) + " ", PriceAdjustmentType.FixedDiscount, 9999999999999999.99m, 0);
        Assert.Equal(200, rule.Name.Length);
        Assert.Equal(9999999999999999.99m, rule.Value);
    }

    private static PriceRule Rule(decimal value, DateTimeOffset? start = null, DateTimeOffset? end = null) => PriceRule.Create("Sale", PriceAdjustmentType.PercentageDiscount, value, 1, start, end);
}
