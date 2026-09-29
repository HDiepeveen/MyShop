using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerProductWriteTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task RemovingVariantDeletesItsEntireGraphAndPreservesOtherVariant()
    {
        var data = await SqlServerProductData.Seed(database);
        await using (var context = database.CreateContext())
        {
            var repository = new ProductRepository(context);
            var snapshot = (await repository.GetByIdAsync(data.Product.Id, default))!;
            snapshot.Product.RemoveVariant(data.Variant.Id);
            await repository.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, default);
        }
        await using var verification = database.CreateContext();
        var actual = (await new ProductRepository(verification).GetByIdAsync(data.Product.Id, default))!.Product;
        Assert.Equal(data.Other.Id, Assert.Single(actual.Variants).Id);
        Assert.Equal(7, actual.AttributeValues.Count);
        Assert.Single(actual.CategoryIds);
        Assert.False(await verification.ProductVariants.AnyAsync(row => row.Id == data.Variant.Id.Value));
        Assert.False(await verification.ProductVariantAttributeValues.AnyAsync(row => row.ProductVariantId == data.Variant.Id.Value));
        Assert.False(await verification.ProductVariantAttributeMultiChoiceValues.AnyAsync(row => row.ProductVariantId == data.Variant.Id.Value));
        Assert.False(await verification.PriceRules.AnyAsync(row => row.ProductVariantId == data.Variant.Id.Value));
        Assert.Null(await new ProductSkuLookup(verification).FindOwnerAsync(data.Variant.Sku!, default));
    }

    [SqlServerFact]
    public async Task RemovingPriceRulePreservesVariantPriceAndAttributes()
    {
        var data = await SqlServerProductData.Seed(database);
        await using (var context = database.CreateContext())
        {
            var repository = new ProductRepository(context);
            var snapshot = (await repository.GetByIdAsync(data.Product.Id, default))!;
            snapshot.Product.RemoveVariantPriceRule(data.Variant.Id, data.Rule.Id);
            await repository.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, default);
        }
        await using var verification = database.CreateContext();
        var product = (await new ProductRepository(verification).GetByIdAsync(data.Product.Id, default))!.Product;
        var variant = Assert.Single(product.Variants, item => item.Id == data.Variant.Id);
        Assert.Empty(variant.PriceRules);
        Assert.Equal(data.Variant.Price, variant.Price);
        Assert.Equal(7, variant.AttributeValues.Count);
        Assert.False(await verification.PriceRules.AnyAsync(row => row.Id == data.Rule.Id));
    }

    [SqlServerFact]
    public async Task StaleWriteRollsBackChildChangesAndPreservesWinningRevision()
    {
        var data = await SqlServerProductData.Seed(database);
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var winnerRepository = new ProductRepository(first);
        var staleRepository = new ProductRepository(second);
        var winner = (await winnerRepository.GetByIdAsync(data.Product.Id, default))!;
        var stale = (await staleRepository.GetByIdAsync(data.Product.Id, default))!;
        winner.Product.Rename("Winning name");
        var token = await winnerRepository.SaveAsync(winner.Product, winner.ConcurrencyToken, default);
        Assert.NotEqual(winner.ConcurrencyToken, token);
        stale.Product.Rename("Stale name");
        stale.Product.RemoveVariant(data.Variant.Id);
        stale.Product.RemoveFromCategory(data.Category.Id);
        await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            staleRepository.SaveAsync(stale.Product, stale.ConcurrencyToken, default));
        await using var verification = database.CreateContext();
        var actual = (await new ProductRepository(verification).GetByIdAsync(data.Product.Id, default))!;
        Assert.Equal("Winning name", actual.Product.Name);
        Assert.Equal(token, actual.ConcurrencyToken);
        Assert.Equal(2, actual.Product.Variants.Count);
        var variant = Assert.Single(actual.Product.Variants, item => item.Id == data.Variant.Id);
        Assert.Equal(7, variant.AttributeValues.Count);
        Assert.Single(variant.PriceRules);
        Assert.Equal(data.Category.Id, Assert.Single(actual.Product.CategoryIds));
    }

    [SqlServerFact]
    public async Task ProductDeletionCascadesDependentsButPreservesTypeAndCategory()
    {
        var data = await SqlServerProductData.Seed(database);
        await using (var context = database.CreateContext())
        {
            var repository = new ProductRepository(context);
            Assert.True(await repository.DeleteAsync(data.Product.Id, default));
            Assert.False(await repository.DeleteAsync(data.Product.Id, default));
        }
        await using var verification = database.CreateContext();
        Assert.Null(await new ProductRepository(verification).GetByIdAsync(data.Product.Id, default));
        Assert.False(await verification.ProductVariants.AnyAsync(row => row.ProductId == data.Product.Id.Value));
        Assert.False(await verification.ProductCategories.AnyAsync(row => row.ProductId == data.Product.Id.Value));
        Assert.False(await verification.ProductAttributeValues.AnyAsync(row => row.ProductId == data.Product.Id.Value));
        Assert.False(await verification.ProductAttributeMultiChoiceValues.AnyAsync(row => row.ProductId == data.Product.Id.Value));
        Assert.False(await verification.ProductVariantAttributeValues.AnyAsync(row => row.ProductVariantId == data.Variant.Id.Value));
        Assert.False(await verification.ProductVariantAttributeMultiChoiceValues.AnyAsync(row => row.ProductVariantId == data.Variant.Id.Value));
        Assert.False(await verification.PriceRules.AnyAsync(row => row.ProductVariantId == data.Variant.Id.Value));
        Assert.True(await verification.Categories.AnyAsync(row => row.Id == data.Category.Id.Value));
        Assert.True(await verification.ProductTypes.AnyAsync(row => row.Id == data.Type.Id.Value));
    }
}
