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
    public void ReadAndSynchronize_RejectInvalidRulesBeforeAnyMutation(string corruption)
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
        Assert.Throws<InvalidOperationException>(() => ProductPersistenceMapper.ToSnapshot(persistence));

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToSnapshot_PreservesAppliedRuleAcrossDifferentRowOrders(bool reverse)
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        product.SetVariantPrice(variant.Id, Money.Create(100m, "EUR"));
        var at = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));
        product.AddVariantPriceRule(variant.Id, PriceRule.Create("First", PriceAdjustmentType.FixedDiscount, 10m, 5));
        product.AddVariantPriceRule(variant.Id, PriceRule.Create("Second", PriceAdjustmentType.PercentageDiscount, 25m, 5, at, at));
        var expectedRule = variant.GetApplicablePriceRule(at)!;
        var persistence = new ProductPersistence { Id = product.Id.Value, Version = Guid.NewGuid() };
        ProductPersistenceSynchronizer.Synchronize(product, persistence);
        var row = persistence.Variants.Single();
        row.PriceRules = (reverse ? row.PriceRules.OrderByDescending(rule => rule.Id) :
            row.PriceRules.OrderBy(rule => rule.Id)).ToList();
        var orderedRows = row.PriceRules.ToArray();

        var snapshot = ProductPersistenceMapper.ToSnapshot(persistence);
        var restored = snapshot.Product.Variants.Single();

        Assert.Equal(expectedRule.Id, restored.GetApplicablePriceRule(at.ToUniversalTime())!.Id);
        Assert.Equal(variant.CalculatePrice(at), restored.CalculatePrice(at.ToUniversalTime()));
        Assert.Equal(persistence.Version, snapshot.ConcurrencyToken.Revision);
        Assert.Equal(orderedRows, row.PriceRules);
    }

    [Fact]
    public void ToSnapshot_PreservesCompletePriceRulePayloadAndDoesNotMutateRows()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        product.SetVariantPrice(variant.Id, Money.Create(25m, "EUR"));
        var start = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.FromHours(2));
        var rule = PriceRule.Create("Sale", PriceAdjustmentType.PercentageDiscount, 12.5m, -1, start, start.AddDays(1));
        product.AddVariantPriceRule(variant.Id, rule);
        var persistence = new ProductPersistence { Id = product.Id.Value, Version = Guid.NewGuid() };
        ProductPersistenceSynchronizer.Synchronize(product, persistence);
        var row = persistence.Variants.Single().PriceRules.Single();

        var snapshot = ProductPersistenceMapper.ToSnapshot(persistence);

        var restoredVariant = snapshot.Product.Variants.Single();
        var restored = Assert.Single(restoredVariant.PriceRules);
        Assert.Equal(variant.Price, restoredVariant.Price);
        Assert.Equal(rule.Id, restored.Id);
        Assert.Equal(rule.Name, restored.Name);
        Assert.Equal(rule.AdjustmentType, restored.AdjustmentType);
        Assert.Equal(rule.Value, restored.Value);
        Assert.Equal(rule.Priority, restored.Priority);
        Assert.Equal(rule.StartsAt, restored.StartsAt);
        Assert.Equal(rule.EndsAt, restored.EndsAt);
        Assert.Same(row, persistence.Variants.Single().PriceRules.Single());
        Assert.Equal(rule.Id, row.Id);
        Assert.Equal(persistence.Version, snapshot.ConcurrencyToken.Revision);
    }
}
