using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class PriceRuleOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Add_RejectsIdentityAlreadyOwnedByAnyVariantWithoutMutation(bool sameVariant)
    {
        var product = Product.Create("Product", ProductTypeId.New(), "First");
        var first = product.Variants.Single();
        var second = product.AddVariant("Second");
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 0);
        product.AddVariantPriceRule(first.Id, rule);

        Assert.Throws<InvalidOperationException>(() =>
            product.AddVariantPriceRule(sameVariant ? first.Id : second.Id, rule));

        Assert.Same(rule, Assert.Single(first.PriceRules));
        Assert.Empty(second.PriceRules);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rehydrate_RejectsDuplicateRuleIdentityAcrossVariants(bool sameInstance)
    {
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 0);
        var duplicate = sameInstance ? rule : PriceRule.Rehydrate(
            rule.Id, "Different payload", PriceAdjustmentType.PercentageDiscount, 10m, 5, null, null);
        var first = ProductVariant.Rehydrate(ProductVariantId.New(), "First", null, [], null, [rule]);
        var second = ProductVariant.Rehydrate(ProductVariantId.New(), "Second", null, [], null, [duplicate]);
        Assert.Throws<InvalidOperationException>(() =>
            Product.Rehydrate(ProductId.New(), ProductTypeId.New(), "Product", [first, second], [], []));
        Assert.Same(rule, Assert.Single(first.PriceRules));
        Assert.Same(duplicate, Assert.Single(second.PriceRules));
    }

    [Fact]
    public void Rehydrate_AllowsSeparateRulesWithSamePayload()
    {
        var first = ProductVariant.Rehydrate(ProductVariantId.New(), "First", null, [], null,
            [PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 0)]);
        var second = ProductVariant.Rehydrate(ProductVariantId.New(), "Second", null, [], null,
            [PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 0)]);
        var product = Product.Rehydrate(ProductId.New(), ProductTypeId.New(), "Product", [first, second], [], []);
        Assert.Equal(new[] { first, second }, product.Variants);
    }

    [Fact]
    public void Add_AllowsIdenticalPayloadWithIndependentIdentities()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "First");
        var first = product.Variants.Single();
        var second = product.AddVariant("Second");
        product.AddVariantPriceRule(first.Id, PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 0));
        product.AddVariantPriceRule(second.Id, PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 0));
        Assert.NotEqual(first.PriceRules.Single().Id, second.PriceRules.Single().Id);
    }

    [Fact]
    public void Add_RejectsNullAndMissingVariantWithoutMutation()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "First");
        var variant = product.Variants.Single();
        Assert.Throws<ArgumentNullException>(() => product.AddVariantPriceRule(variant.Id, null!));
        Assert.Throws<InvalidOperationException>(() => product.AddVariantPriceRule(ProductVariantId.New(),
            PriceRule.Create("Sale", PriceAdjustmentType.FixedDiscount, 2m, 0)));
        Assert.Empty(variant.PriceRules);
    }
}
