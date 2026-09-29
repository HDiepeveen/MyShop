using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerProductRemovalTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task AlreadyTrackedGraphRemovesAttributesTheirChoicesAndCategoryLinks()
    {
        var data = await SqlServerProductData.Seed(database);
        var productDefinition = data.Definitions[(AttributeScope.Product, AttributeDataType.MultiChoice)];
        var variantDefinition = data.Definitions[(AttributeScope.Variant, AttributeDataType.MultiChoice)];
        await using (var context = database.CreateContext())
        {
            await ProductRepository.CompleteGraph(context.Products).SingleAsync(row => row.Id == data.Product.Id.Value);
            var repository = new ProductRepository(context);
            var snapshot = (await repository.GetByIdAsync(data.Product.Id, default))!;
            snapshot.Product.RemoveAttributeValue(productDefinition);
            snapshot.Product.RemoveVariantAttributeValue(data.Variant.Id, variantDefinition);
            snapshot.Product.RemoveFromCategory(data.Category.Id);
            await repository.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, default);
        }
        await using var verification = database.CreateContext();
        var actual = (await new ProductRepository(verification).GetByIdAsync(data.Product.Id, default))!.Product;
        Assert.Equal(6, actual.AttributeValues.Count);
        Assert.DoesNotContain(actual.AttributeValues, value => value.AttributeDefinitionId == productDefinition);
        var variant = Assert.Single(actual.Variants, item => item.Id == data.Variant.Id);
        Assert.Equal(6, variant.AttributeValues.Count);
        Assert.DoesNotContain(variant.AttributeValues, value => value.AttributeDefinitionId == variantDefinition);
        Assert.Empty(actual.CategoryIds);
        Assert.False(await verification.ProductAttributeMultiChoiceValues.AnyAsync(row => row.ProductId == data.Product.Id.Value));
        Assert.False(await verification.ProductVariantAttributeMultiChoiceValues.AnyAsync(row => row.ProductVariantId == data.Variant.Id.Value));
        Assert.True(await verification.Categories.AnyAsync(row => row.Id == data.Category.Id.Value));
        Assert.Single(variant.PriceRules);
        Assert.Equal(2, actual.Variants.Count);
    }

    [SqlServerFact]
    public async Task RepeatedChoiceReplacementInSameContextPersistsOrderWithoutStaleRows()
    {
        var data = await SqlServerProductData.Seed(database);
        var productDefinition = data.Definitions[(AttributeScope.Product, AttributeDataType.MultiChoice)];
        var variantDefinition = data.Definitions[(AttributeScope.Variant, AttributeDataType.MultiChoice)];
        await using (var context = database.CreateContext())
        {
            var repository = new ProductRepository(context);
            var snapshot = (await repository.GetByIdAsync(data.Product.Id, default))!;
            var token = snapshot.ConcurrencyToken;
            foreach (var choices in new[] { new[] { "Red", "Green", "Blue" }, new[] { "Blue", "Red" }, new[] { "Red" } })
            {
                snapshot.Product.SetAttributeValue(MultiChoiceAttributeValue.Create(productDefinition, choices.Select(ChoiceValue.Create)));
                snapshot.Product.SetVariantAttributeValue(data.Variant.Id,
                    MultiChoiceAttributeValue.Create(variantDefinition, choices.Select(ChoiceValue.Create)));
                token = await repository.SaveAsync(snapshot.Product, token, default);
                await using var verification = database.CreateContext();
                Assert.Equal(choices, await verification.ProductAttributeMultiChoiceValues
                    .Where(row => row.ProductId == data.Product.Id.Value).OrderBy(row => row.Ordinal).Select(row => row.Value).ToArrayAsync());
                Assert.Equal(choices, await verification.ProductVariantAttributeMultiChoiceValues
                    .Where(row => row.ProductVariantId == data.Variant.Id.Value).OrderBy(row => row.Ordinal).Select(row => row.Value).ToArrayAsync());
                Assert.Equal(Enumerable.Range(0, choices.Length), await verification.ProductAttributeMultiChoiceValues
                    .Where(row => row.ProductId == data.Product.Id.Value).OrderBy(row => row.Ordinal).Select(row => row.Ordinal).ToArrayAsync());
            }
        }
    }
}
