using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class ProductPriceRuleValidationTests
{
    [Theory]
    [InlineData("null collection")]
    [InlineData("null entry")]
    [InlineData("empty ID")]
    [InlineData("duplicate ID")]
    [InlineData("foreign owner")]
    [InlineData("empty owner")]
    [InlineData("duplicate across variants")]
    public void Synchronize_RejectsInvalidRulesBeforeAnyMutation(string corruption)
    {
        var product = Product.Create("Original", ProductTypeId.New(), "First");
        var first = product.Variants.Single();
        var second = product.AddVariant("Second");
        product.SetVariantPrice(first.Id, Money.Create(20m, "EUR"));
        product.AddVariantPriceRule(first.Id, PriceRule.Create("First sale", PriceAdjustmentType.FixedDiscount, 1m, 0));
        product.AddVariantPriceRule(second.Id, PriceRule.Create("Second sale", PriceAdjustmentType.FixedDiscount, 2m, 0));
        var persistence = new ProductPersistence { Id = product.Id.Value, Version = Guid.NewGuid() };
        ProductPersistenceSynchronizer.Synchronize(product, persistence);
        var firstRow = persistence.Variants.First();
        var secondRow = persistence.Variants.Last();
        var rule = secondRow.PriceRules.Single();
        var version = persistence.Version;
        switch (corruption)
        {
            case "null collection": secondRow.PriceRules = null!; break;
            case "null entry": secondRow.PriceRules.Add(null!); break;
            case "empty ID": rule.Id = Guid.Empty; break;
            case "duplicate ID": secondRow.PriceRules.Add(new PriceRulePersistence { Id = rule.Id, ProductVariantId = secondRow.Id }); break;
            case "foreign owner": rule.ProductVariantId = Guid.NewGuid(); break;
            case "empty owner": rule.ProductVariantId = Guid.Empty; break;
            case "duplicate across variants": rule.Id = firstRow.PriceRules.Single().Id; break;
        }
        product.Rename("Changed");
        product.SetVariantPrice(first.Id, Money.Create(99m, "EUR"));
        product.RemoveVariant(second.Id);

        Assert.Throws<InvalidOperationException>(() => ProductPersistenceSynchronizer.Synchronize(product, persistence));

        Assert.Equal("Original", persistence.Name);
        Assert.Equal(20m, firstRow.PriceAmount);
        Assert.Equal(version, persistence.Version);
        Assert.Equal(2, persistence.Variants.Count);
        Assert.Same(secondRow, persistence.Variants.Last());
    }
}
