using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence;

public sealed class CatalogPersistenceModelContractTests
{
    public static TheoryData<Type, string> NeverGeneratedProperties => new()
    {
        { typeof(ProductPersistence), "Id" },
        { typeof(ProductPersistence), "Version" },
        { typeof(ProductVariantPersistence), "Id" },
        { typeof(ProductTypePersistence), "Id" },
        { typeof(AttributeDefinitionPersistence), "Id" },
        { typeof(CategoryPersistence), "Id" },
        { typeof(ProductCategoryPersistence), "ProductId" },
        { typeof(ProductCategoryPersistence), "CategoryId" },
        { typeof(ProductAttributeValuePersistence), "ProductId" },
        { typeof(ProductAttributeValuePersistence), "AttributeDefinitionId" },
        { typeof(ProductAttributeMultiChoiceValuePersistence), "ProductId" },
        { typeof(ProductAttributeMultiChoiceValuePersistence), "AttributeDefinitionId" },
        { typeof(ProductVariantAttributeValuePersistence), "ProductVariantId" },
        { typeof(ProductVariantAttributeValuePersistence), "AttributeDefinitionId" },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), "ProductVariantId" },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), "AttributeDefinitionId" },
        { typeof(ProductAttributeValuePersistence), "Ordinal" },
        { typeof(ProductVariantAttributeValuePersistence), "Ordinal" },
        { typeof(ProductAttributeMultiChoiceValuePersistence), "Ordinal" },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), "Ordinal" }
    };
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

    public static TheoryData<Type, string, bool> PropertyNullability => new()
    {
        { typeof(ProductPersistence), "Id", false },
        { typeof(ProductPersistence), "ProductTypeId", false },
        { typeof(ProductPersistence), "Name", false },
        { typeof(ProductPersistence), "Version", false },
        { typeof(ProductVariantPersistence), "Id", false },
        { typeof(ProductVariantPersistence), "ProductId", false },
        { typeof(ProductVariantPersistence), "Name", false },
        { typeof(ProductVariantPersistence), "Sku", true },
        { typeof(ProductVariantPersistence), "Ordinal", false },
        { typeof(CategoryPersistence), "Id", false },
        { typeof(CategoryPersistence), "Name", false },
        { typeof(CategoryPersistence), "ParentCategoryId", true },
        { typeof(ProductTypePersistence), "Id", false },
        { typeof(ProductTypePersistence), "Name", false },
        { typeof(AttributeDefinitionPersistence), "Id", false },
        { typeof(AttributeDefinitionPersistence), "ProductTypeId", false },
        { typeof(AttributeDefinitionPersistence), "Code", false },
        { typeof(AttributeDefinitionPersistence), "DisplayName", false },
        { typeof(ProductAttributeValuePersistence), "TextValue", true },
        { typeof(ProductVariantAttributeValuePersistence), "TextValue", true }
    };

    public static TheoryData<Type, Type, DeleteBehavior> Relationships => new()
    {
        { typeof(ProductPersistence), typeof(ProductTypePersistence), DeleteBehavior.NoAction },
        { typeof(ProductVariantPersistence), typeof(ProductPersistence), DeleteBehavior.Cascade },
        { typeof(CategoryPersistence), typeof(CategoryPersistence), DeleteBehavior.Restrict },
        { typeof(AttributeDefinitionPersistence), typeof(ProductTypePersistence), DeleteBehavior.Cascade },
        { typeof(ProductCategoryPersistence), typeof(ProductPersistence), DeleteBehavior.Cascade },
        { typeof(ProductAttributeValuePersistence), typeof(ProductPersistence), DeleteBehavior.Cascade },
        { typeof(ProductAttributeMultiChoiceValuePersistence), typeof(ProductAttributeValuePersistence), DeleteBehavior.Cascade },
        { typeof(ProductVariantAttributeValuePersistence), typeof(ProductVariantPersistence), DeleteBehavior.Cascade },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), typeof(ProductVariantAttributeValuePersistence), DeleteBehavior.Cascade }
    };

    public static TheoryData<Type, string, string> ColumnTypes => new()
    {
        { typeof(ProductAttributeValuePersistence), "DataType", "int" },
        { typeof(ProductAttributeValuePersistence), "Ordinal", "int" },
        { typeof(ProductAttributeValuePersistence), "TextValue", "nvarchar(max)" },
        { typeof(ProductAttributeValuePersistence), "IntegerValue", "bigint" },
        { typeof(ProductAttributeValuePersistence), "DecimalScale", "tinyint" },
        { typeof(ProductAttributeValuePersistence), "BooleanValue", "bit" },
        { typeof(ProductAttributeValuePersistence), "DateValue", "date" },
        { typeof(ProductAttributeValuePersistence), "ChoiceValue", "nvarchar(max)" },
        { typeof(ProductVariantAttributeValuePersistence), "DataType", "int" },
        { typeof(ProductVariantAttributeValuePersistence), "Ordinal", "int" },
        { typeof(ProductVariantAttributeValuePersistence), "TextValue", "nvarchar(max)" },
        { typeof(ProductVariantAttributeValuePersistence), "IntegerValue", "bigint" },
        { typeof(ProductVariantAttributeValuePersistence), "DecimalScale", "tinyint" },
        { typeof(ProductVariantAttributeValuePersistence), "BooleanValue", "bit" },
        { typeof(ProductVariantAttributeValuePersistence), "DateValue", "date" },
        { typeof(ProductVariantAttributeValuePersistence), "ChoiceValue", "nvarchar(max)" }
    };

    [Theory]
    [MemberData(nameof(TableMappings))]
    public void Entity_UsesExpectedTable(Type entityType, string tableName)
    {
        using var context = CreateContext();
        Assert.Equal(tableName, context.Model.FindEntityType(entityType)!.GetTableName());
    }

    [Theory]
    [MemberData(nameof(NeverGeneratedProperties))]
    public void Property_IsApplicationManaged(Type entityType, string propertyName)
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
    }

    [Theory]
    [MemberData(nameof(PrimaryKeys))]
    public void Entity_UsesExpectedPrimaryKey(Type entityType, string[] propertyNames)
    {
        using var context = CreateContext();
        var key = context.Model.FindEntityType(entityType)!.FindPrimaryKey()!;
        Assert.Equal(propertyNames, key.Properties.Select(property => property.Name));
    }

    [Theory]
    [MemberData(nameof(PropertyNullability))]
    public void Property_UsesExpectedNullability(Type entityType, string propertyName, bool nullable)
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;
        Assert.Equal(nullable, property.IsNullable);
    }

    [Theory]
    [MemberData(nameof(Relationships))]
    public void Relationship_UsesExpectedDeleteBehavior(
        Type dependentType, Type principalType, DeleteBehavior deleteBehavior)
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(dependentType)!;
        var foreignKey = Assert.Single(entity.GetForeignKeys(),
            candidate => candidate.PrincipalEntityType.ClrType == principalType);
        Assert.Equal(deleteBehavior, foreignKey.DeleteBehavior);
    }

    [Theory]
    [MemberData(nameof(ColumnTypes))]
    public void Property_UsesExpectedSqlServerColumnType(
        Type entityType, string propertyName, string columnType)
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;
        Assert.Equal(columnType, property.GetColumnType());
    }

    private static MyShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyShopDbContext>().UseSqlServer().Options;
        return new MyShopDbContext(options);
    }
}
