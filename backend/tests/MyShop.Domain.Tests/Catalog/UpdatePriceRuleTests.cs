using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class UpdatePriceRuleTests
{
    [Fact]
    public void Update_PreservesIdentityOrderAndOtherVariantWhileReplacingPayload()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "First");
        var first = product.Variants.Single();
        var second = product.AddVariant("Second");
        var original = PriceRule.Create("Old", PriceAdjustmentType.FixedDiscount, 1m, 0);
        var retained = PriceRule.Create("Other", PriceAdjustmentType.FixedDiscount, 2m, 1);
        product.AddVariantPriceRule(first.Id, original);
        product.AddVariantPriceRule(first.Id, retained);
        var otherVariantRule = PriceRule.Create("Second rule", PriceAdjustmentType.FixedDiscount, 3m, 0);
        product.AddVariantPriceRule(second.Id, otherVariantRule);
        var at = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.True(product.UpdateVariantPriceRule(first.Id, original.Id, " Updated ",
            PriceAdjustmentType.PercentageDiscount, 12.345m, 5, at, at.AddDays(1)));

        var changed = first.PriceRules.First();
        Assert.Equal(original.Id, changed.Id);
        Assert.NotSame(original, changed);
        Assert.Equal("Updated", changed.Name);
        Assert.Equal(12.34m, changed.Value);
        Assert.Equal(PriceAdjustmentType.PercentageDiscount, changed.AdjustmentType);
        Assert.Equal(5, changed.Priority);
        Assert.Equal(at, changed.StartsAt);
        Assert.Equal(at.AddDays(1), changed.EndsAt);
        Assert.Same(retained, first.PriceRules.Last());
        Assert.Same(otherVariantRule, Assert.Single(second.PriceRules));
        Assert.Equal("Old", original.Name);
    }

    [Fact]
    public void Update_NormalizedNoOpRetainsOriginalInstance()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 12.34m, 1);
        product.AddVariantPriceRule(variant.Id, rule);
        Assert.False(product.UpdateVariantPriceRule(variant.Id, rule.Id, " Sale ",
            rule.AdjustmentType, 12.345m, 1));
        Assert.Same(rule, Assert.Single(variant.PriceRules));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("value")]
    [InlineData("type")]
    [InlineData("period")]
    public void Update_InvalidPayloadLeavesOriginalRuleUntouched(string field)
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 5m, 1);
        product.AddVariantPriceRule(variant.Id, rule);
        var at = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.ThrowsAny<ArgumentException>(() => product.UpdateVariantPriceRule(variant.Id, rule.Id,
            field == "name" ? " " : "Updated",
            field == "type" ? (PriceAdjustmentType)99 : PriceAdjustmentType.PercentageDiscount,
            field == "value" ? 101m : 10m, 2, at, field == "period" ? at.AddDays(-1) : null));
        Assert.Same(rule, Assert.Single(variant.PriceRules));
    }

    [Fact]
    public void Update_RejectsMissingOrEmptyIdentity()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var id = product.Variants.Single().Id;
        Assert.Throws<ArgumentException>(() => product.UpdateVariantPriceRule(id, Guid.Empty, "Sale", PriceAdjustmentType.FixedDiscount, 1m, 0));
        Assert.Throws<InvalidOperationException>(() => product.UpdateVariantPriceRule(id, Guid.NewGuid(), "Sale", PriceAdjustmentType.FixedDiscount, 1m, 0));
        Assert.Throws<InvalidOperationException>(() => product.UpdateVariantPriceRule(ProductVariantId.New(), Guid.NewGuid(), "Sale", PriceAdjustmentType.FixedDiscount, 1m, 0));
    }
}
