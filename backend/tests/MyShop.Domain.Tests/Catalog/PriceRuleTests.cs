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
    private static PriceRule Rule(decimal value, DateTimeOffset? start = null, DateTimeOffset? end = null) => PriceRule.Create("Sale", PriceAdjustmentType.PercentageDiscount, value, 1, start, end);
}
