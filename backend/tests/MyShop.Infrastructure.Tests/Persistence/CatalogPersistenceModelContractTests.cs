using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence;

public sealed class CatalogPersistenceModelContractTests
{
    public static TheoryData<Type, string> TableMappings => new()
    {
        { typeof(ProductPersistence), "Products" },
        { typeof(ProductVariantPersistence), "ProductVariants" },
        { typeof(ProductCategoryPersistence), "ProductCategories" },
        { typeof(ProductAttributeValuePersistence), "ProductAttributeValues" },
        { typeof(ProductAttributeMultiChoiceValuePersistence), "ProductAttributeMultiChoiceValues" },
        { typeof(ProductVariantAttributeValuePersistence), "ProductVariantAttributeValues" },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), "ProductVariantAttributeMultiChoiceValues" },
        { typeof(ProductTypePersistence), "ProductTypes" },
        { typeof(AttributeDefinitionPersistence), "AttributeDefinitions" },
        { typeof(CategoryPersistence), "Categories" }
    };

    public static TheoryData<Type, string[]> PrimaryKeys => new()
    {
        { typeof(ProductPersistence), ["Id"] },
        { typeof(ProductVariantPersistence), ["Id"] },
        { typeof(ProductCategoryPersistence), ["ProductId", "CategoryId"] },
        { typeof(ProductAttributeValuePersistence), ["ProductId", "AttributeDefinitionId"] },
        { typeof(ProductAttributeMultiChoiceValuePersistence), ["ProductId", "AttributeDefinitionId", "Ordinal"] },
        { typeof(ProductVariantAttributeValuePersistence), ["ProductVariantId", "AttributeDefinitionId"] },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), ["ProductVariantId", "AttributeDefinitionId", "Ordinal"] },
        { typeof(ProductTypePersistence), ["Id"] },
        { typeof(AttributeDefinitionPersistence), ["Id"] },
        { typeof(CategoryPersistence), ["Id"] }
    };

    [Theory]
    [MemberData(nameof(TableMappings))]
    public void Entity_UsesExpectedTable(Type entityType, string tableName)
    {
        using var context = CreateContext();
        Assert.Equal(tableName, context.Model.FindEntityType(entityType)!.GetTableName());
    }

    [Theory]
    [MemberData(nameof(PrimaryKeys))]
    public void Entity_UsesExpectedPrimaryKey(Type entityType, string[] propertyNames)
    {
        using var context = CreateContext();
        var key = context.Model.FindEntityType(entityType)!.FindPrimaryKey()!;
        Assert.Equal(propertyNames, key.Properties.Select(property => property.Name));
    }

    private static MyShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyShopDbContext>().UseSqlServer().Options;
        return new MyShopDbContext(options);
    }
}
