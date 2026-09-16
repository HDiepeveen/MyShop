using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class ProductPersistenceSynchronizerTrackingTests
{
    [Fact]
    public void Synchronize_FullyTrackedGraph_MarksRemovedDependentsAsDeleted()
    {
        var scenario = CreateScenario();
        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options;

        using var context = new MyShopDbContext(options);
        context.Attach(scenario.Persistence);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.All(context.ChangeTracker.Entries(), entry =>
            Assert.Equal(EntityState.Unchanged, entry.State));

        ProductPersistenceSynchronizer.Synchronize(scenario.Product, scenario.Persistence);
        context.ChangeTracker.DetectChanges();

        AssertDeleted(context, scenario.RemovedVariant);
        AssertDeleted(context, scenario.RemovedCategory);
        AssertDeleted(context, scenario.RemovedProductAttribute);
        AssertDeleted(context, scenario.RemovedVariantAttribute);
        AssertDeleted(context, scenario.RemovedProductChild);
        AssertDeleted(context, scenario.RemovedVariantChild);
    }

    private static TrackingScenario CreateScenario()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Retained variant");
        var variant = product.Variants.Single();
        var categoryId = CategoryId.New();
        var productDefinitionId = AttributeDefinitionId.New();
        var variantDefinitionId = AttributeDefinitionId.New();

        product.AssignToCategory(categoryId);
        product.SetAttributeValue(MultiChoiceAttributeValue.Create(
            productDefinitionId, [ChoiceValue.Create("retained product choice")]));
        product.SetVariantAttributeValue(variant.Id, MultiChoiceAttributeValue.Create(
            variantDefinitionId, [ChoiceValue.Create("retained variant choice")]));

        var persistence = new ProductPersistence
        {
            Id = product.Id.Value,
            ProductTypeId = product.ProductTypeId.Value,
            Name = product.Name,
            Version = Guid.NewGuid()
        };

        var retainedVariant = new ProductVariantPersistence
        {
            Id = variant.Id.Value,
            ProductId = persistence.Id,
            Product = persistence,
            Name = variant.Name,
            Ordinal = 0
        };
        var removedVariant = new ProductVariantPersistence
        {
            Id = Guid.NewGuid(),
            ProductId = persistence.Id,
            Product = persistence,
            Name = "Removed variant",
            Ordinal = 1
        };
        persistence.Variants.Add(retainedVariant);
        persistence.Variants.Add(removedVariant);

        var retainedCategory = Category(persistence, categoryId.Value, 0);
        var removedCategory = Category(persistence, Guid.NewGuid(), 1);
        persistence.Categories.Add(retainedCategory);
        persistence.Categories.Add(removedCategory);

        var retainedProductAttribute = ProductAttribute(persistence, productDefinitionId.Value, 0);
        var removedProductAttribute = ProductAttribute(persistence, Guid.NewGuid(), 1);
        var retainedProductChild = ProductChild(retainedProductAttribute, 0, "old product choice");
        var removedProductChild = ProductChild(retainedProductAttribute, 1, "removed product choice");
        retainedProductAttribute.MultiChoiceValues.Add(retainedProductChild);
        retainedProductAttribute.MultiChoiceValues.Add(removedProductChild);
        persistence.AttributeValues.Add(retainedProductAttribute);
        persistence.AttributeValues.Add(removedProductAttribute);

        var retainedVariantAttribute = VariantAttribute(retainedVariant, variantDefinitionId.Value, 0);
        var removedVariantAttribute = VariantAttribute(retainedVariant, Guid.NewGuid(), 1);
        var retainedVariantChild = VariantChild(retainedVariantAttribute, 0, "old variant choice");
        var removedVariantChild = VariantChild(retainedVariantAttribute, 1, "removed variant choice");
        retainedVariantAttribute.MultiChoiceValues.Add(retainedVariantChild);
        retainedVariantAttribute.MultiChoiceValues.Add(removedVariantChild);
        retainedVariant.AttributeValues.Add(retainedVariantAttribute);
        retainedVariant.AttributeValues.Add(removedVariantAttribute);

        return new TrackingScenario(
            product,
            persistence,
            removedVariant,
            removedCategory,
            removedProductAttribute,
            removedVariantAttribute,
            removedProductChild,
            removedVariantChild);
    }

    private static void AssertDeleted(MyShopDbContext context, object entity) =>
        Assert.Equal(EntityState.Deleted, context.Entry(entity).State);

    private static ProductCategoryPersistence Category(
        ProductPersistence product, Guid categoryId, int ordinal) => new()
    {
        ProductId = product.Id,
        CategoryId = categoryId,
        Product = product,
        Ordinal = ordinal
    };

    private static ProductAttributeValuePersistence ProductAttribute(
        ProductPersistence product, Guid definitionId, int ordinal) => new()
    {
        ProductId = product.Id,
        AttributeDefinitionId = definitionId,
        Product = product,
        DataType = AttributeDataType.MultiChoice,
        Ordinal = ordinal
    };

    private static ProductVariantAttributeValuePersistence VariantAttribute(
        ProductVariantPersistence variant, Guid definitionId, int ordinal) => new()
    {
        ProductVariantId = variant.Id,
        AttributeDefinitionId = definitionId,
        ProductVariant = variant,
        DataType = AttributeDataType.MultiChoice,
        Ordinal = ordinal
    };

    private static ProductAttributeMultiChoiceValuePersistence ProductChild(
        ProductAttributeValuePersistence parent, int ordinal, string value) => new()
    {
        ProductId = parent.ProductId,
        AttributeDefinitionId = parent.AttributeDefinitionId,
        AttributeValue = parent,
        Ordinal = ordinal,
        Value = value
    };

    private static ProductVariantAttributeMultiChoiceValuePersistence VariantChild(
        ProductVariantAttributeValuePersistence parent, int ordinal, string value) => new()
    {
        ProductVariantId = parent.ProductVariantId,
        AttributeDefinitionId = parent.AttributeDefinitionId,
        AttributeValue = parent,
        Ordinal = ordinal,
        Value = value
    };

    private sealed record TrackingScenario(
        Product Product,
        ProductPersistence Persistence,
        ProductVariantPersistence RemovedVariant,
        ProductCategoryPersistence RemovedCategory,
        ProductAttributeValuePersistence RemovedProductAttribute,
        ProductVariantAttributeValuePersistence RemovedVariantAttribute,
        ProductAttributeMultiChoiceValuePersistence RemovedProductChild,
        ProductVariantAttributeMultiChoiceValuePersistence RemovedVariantChild);
}
