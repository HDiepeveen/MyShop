using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerProductReadTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task CompleteGraphRoundTripsAllAttributeTypesAndPriceRules()
    {
        var data = await SqlServerProductData.Seed(database);
        await using var context = database.CreateContext();
        var snapshot = await new ProductRepository(context).GetByIdAsync(data.Product.Id, default);
        Assert.NotNull(snapshot);
        Assert.Equal(data.Token, snapshot.ConcurrencyToken);
        Assert.Equal(data.Product.Name, snapshot.Product.Name);
        Assert.Equal(data.Type.Id, snapshot.Product.ProductTypeId);
        Assert.Equal(data.Category.Id, Assert.Single(snapshot.Product.CategoryIds));
        Assert.Equal(2, snapshot.Product.Variants.Count);
        Assert.Equal(data.Product.AttributeValues.OrderBy(value => value.AttributeDefinitionId.Value),
            snapshot.Product.AttributeValues.OrderBy(value => value.AttributeDefinitionId.Value));
        var variant = Assert.Single(snapshot.Product.Variants, item => item.Id == data.Variant.Id);
        Assert.Equal(data.Variant.Sku, variant.Sku);
        Assert.Equal(data.Variant.Price, variant.Price);
        Assert.Equal(data.Variant.AttributeValues.OrderBy(value => value.AttributeDefinitionId.Value),
            variant.AttributeValues.OrderBy(value => value.AttributeDefinitionId.Value));
        var rule = Assert.Single(variant.PriceRules);
        Assert.Equal(data.Rule.Id, rule.Id);
        Assert.Equal(data.Rule.Value, rule.Value);
        Assert.Equal(data.Rule.StartsAt, rule.StartsAt);
        Assert.Null(rule.EndsAt);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [SqlServerFact]
    public async Task SkuLookupResolvesStoredVariantAndMissingSkuReturnsNull()
    {
        var data = await SqlServerProductData.Seed(database);
        await using var context = database.CreateContext();
        var lookup = new ProductSkuLookup(context);
        var owner = await lookup.FindOwnerAsync(data.Variant.Sku!, default);
        Assert.NotNull(owner);
        Assert.Equal(data.Product.Id, owner.ProductId);
        Assert.Equal(data.Variant.Id, owner.ProductVariantId);
        Assert.Null(await lookup.FindOwnerAsync(Sku.Create("MISSING-" + Guid.NewGuid().ToString("N")), default));
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
