using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ApplicablePriceRuleTests
{
    private static readonly DateTimeOffset At = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void Selection_UsesInclusivePeriodAndMatchesCalculatedPrice(int hours, bool scheduled)
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        product.SetVariantPrice(variant.Id, Money.Create(100m, "EUR"));
        var fallback = PriceRule.Create("Always", PriceAdjustmentType.FixedDiscount, 10m, 0);
        var sale = PriceRule.Create("Sale", PriceAdjustmentType.PercentageDiscount, 25m, 5, At, At.AddHours(1));
        product.AddVariantPriceRule(variant.Id, fallback);
        product.AddVariantPriceRule(variant.Id, sale);
        var instant = At.AddHours(hours).ToUniversalTime();

        var rule = variant.GetApplicablePriceRule(instant);

        Assert.Same(scheduled ? sale : fallback, rule);
        Assert.Equal(rule!.Apply(variant.Price!.Value), variant.CalculatePrice(instant));
    }

    [Fact]
    public void Selection_BreaksPriorityTiesByIdentityRegardlessOfInsertionOrder()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        var rules = new[] {
            PriceRule.Create("A", PriceAdjustmentType.FixedDiscount, 1m, 5),
            PriceRule.Create("B", PriceAdjustmentType.FixedDiscount, 2m, 5)
        }.OrderByDescending(rule => rule.Id).ToArray();
        foreach (var rule in rules) product.AddVariantPriceRule(variant.Id, rule);
        Assert.Same(rules.Last(), variant.GetApplicablePriceRule(At));
        Assert.Equal(rules, variant.PriceRules);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Selection_ReturnsNullWithoutActiveRules(bool expired)
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        product.SetVariantPrice(variant.Id, Money.Create(10m, "EUR"));
        if (expired)
            product.AddVariantPriceRule(variant.Id, PriceRule.Create("Expired",
                PriceAdjustmentType.FixedDiscount, 2m, 0, null, At.AddTicks(-1)));
        Assert.Null(variant.GetApplicablePriceRule(At));
        Assert.Equal(variant.Price, variant.CalculatePrice(At));
    }
}
