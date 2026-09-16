using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence;

public sealed class ProductAttributeValuePersistenceConfigurationTests
{
    [Fact]
    public void ProductAttributeValue_MapsExpectedSchemaAndPayloadColumns() =>
        AssertParentMapping<ProductAttributeValuePersistence>(
            "ProductAttributeValues", ["ProductId", "AttributeDefinitionId"]);

    [Fact]
    public void ProductVariantAttributeValue_MapsExpectedSchemaAndPayloadColumns() =>
        AssertParentMapping<ProductVariantAttributeValuePersistence>(
            "ProductVariantAttributeValues", ["ProductVariantId", "AttributeDefinitionId"]);

    [Fact]
    public void ProductAttributeValue_HasOnlyRequiredProductRelationship() =>
        AssertOwnerRelationship<ProductAttributeValuePersistence, ProductPersistence>(
            "ProductId", "Product", "AttributeValues");

    [Fact]
    public void ProductVariantAttributeValue_HasOnlyRequiredVariantRelationship() =>
        AssertOwnerRelationship<ProductVariantAttributeValuePersistence, ProductVariantPersistence>(
            "ProductVariantId", "ProductVariant", "AttributeValues");

    [Fact]
    public void ProductAttributeMultiChoiceValue_MapsExpectedSchemaAndRelationship() =>
        AssertMultiChoiceMapping<ProductAttributeMultiChoiceValuePersistence,
            ProductAttributeValuePersistence>(
            "ProductAttributeMultiChoiceValues",
            ["ProductId", "AttributeDefinitionId", "Ordinal"],
            ["ProductId", "AttributeDefinitionId"]);

    [Fact]
    public void ProductVariantAttributeMultiChoiceValue_MapsExpectedSchemaAndRelationship() =>
        AssertMultiChoiceMapping<ProductVariantAttributeMultiChoiceValuePersistence,
            ProductVariantAttributeValuePersistence>(
            "ProductVariantAttributeMultiChoiceValues",
            ["ProductVariantId", "AttributeDefinitionId", "Ordinal"],
            ["ProductVariantId", "AttributeDefinitionId"]);

    private static void AssertParentMapping<TEntity>(string tableName, string[] keyNames)
        where TEntity : class
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(TEntity))!;

        Assert.Equal(tableName, entity.GetTableName());
        Assert.Equal(keyNames, entity.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.True(entity.GetProperties().Select(property => property.Name).ToHashSet().SetEquals(
            [keyNames[0], "AttributeDefinitionId", "DataType", "Ordinal", "TextValue",
                "IntegerValue", "DecimalCoefficient", "DecimalScale", "BooleanValue",
                "DateValue", "ChoiceValue"]));
        Assert.All(entity.GetProperties(), property => Assert.NotNull(property.PropertyInfo));

        AssertGuidProperty(entity, keyNames[0]);
        AssertGuidProperty(entity, "AttributeDefinitionId");
        AssertProperty(entity, "DataType", typeof(AttributeDataType), "int", false);
        AssertProperty(entity, "Ordinal", typeof(int), "int", false);
        AssertProperty(entity, "TextValue", typeof(string), "nvarchar(max)", true);
        AssertProperty(entity, "IntegerValue", typeof(long?), "bigint", true);
        var coefficient = AssertProperty(entity, "DecimalCoefficient", typeof(decimal?), "decimal(29,0)", true);
        Assert.Equal(29, coefficient.GetPrecision());
        Assert.Equal(0, coefficient.GetScale());
        AssertProperty(entity, "DecimalScale", typeof(byte?), "tinyint", true);
        AssertProperty(entity, "BooleanValue", typeof(bool?), "bit", true);
        AssertProperty(entity, "DateValue", typeof(DateOnly?), "date", true);
        AssertProperty(entity, "ChoiceValue", typeof(string), "nvarchar(max)", true);

        Assert.DoesNotContain(entity.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Any(property => property.Name == "AttributeDefinitionId"));
        Assert.DoesNotContain(entity.GetNavigations(), navigation =>
            navigation.TargetEntityType.ClrType == typeof(AttributeDefinitionPersistence));
        Assert.All(entity.GetProperties(), property => Assert.False(property.IsConcurrencyToken));
        Assert.Empty(entity.GetIndexes());
    }

    private static void AssertOwnerRelationship<TEntity, TPrincipal>(
        string foreignKeyName,
        string dependentNavigation,
        string principalNavigation)
        where TEntity : class
        where TPrincipal : class
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(TEntity))!;
        var relationship = Assert.Single(entity.GetForeignKeys());

        Assert.Equal(foreignKeyName, Assert.Single(relationship.Properties).Name);
        Assert.Equal(typeof(TPrincipal), relationship.PrincipalEntityType.ClrType);
        Assert.True(relationship.IsRequired);
        Assert.False(relationship.IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior);
        Assert.Equal(dependentNavigation, relationship.DependentToPrincipal!.Name);
        Assert.Equal(principalNavigation, relationship.PrincipalToDependent!.Name);
    }

    private static void AssertMultiChoiceMapping<TEntity, TPrincipal>(
        string tableName,
        string[] keyNames,
        string[] foreignKeyNames)
        where TEntity : class
        where TPrincipal : class
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(TEntity))!;

        Assert.Equal(tableName, entity.GetTableName());
        Assert.Equal(keyNames, entity.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.True(entity.GetProperties().Select(property => property.Name).ToHashSet().SetEquals(
            [keyNames[0], "AttributeDefinitionId", "Ordinal", "Value"]));
        Assert.All(entity.GetProperties(), property => Assert.NotNull(property.PropertyInfo));
        AssertGuidProperty(entity, keyNames[0]);
        AssertGuidProperty(entity, "AttributeDefinitionId");
        AssertProperty(entity, "Ordinal", typeof(int), "int", false);
        var value = AssertProperty(entity, "Value", typeof(string), "nvarchar(max)", false);
        Assert.Null(value.GetMaxLength());

        var relationship = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(foreignKeyNames, relationship.Properties.Select(property => property.Name));
        Assert.Equal(typeof(TPrincipal), relationship.PrincipalEntityType.ClrType);
        Assert.Equal(foreignKeyNames,
            relationship.PrincipalKey.Properties.Select(property => property.Name));
        Assert.True(relationship.IsRequired);
        Assert.False(relationship.IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior);
        Assert.Equal("AttributeValue", relationship.DependentToPrincipal!.Name);
        Assert.Equal("MultiChoiceValues", relationship.PrincipalToDependent!.Name);

        Assert.DoesNotContain(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(AttributeDefinitionPersistence));
        Assert.DoesNotContain(entity.GetNavigations(), navigation =>
            navigation.TargetEntityType.ClrType == typeof(AttributeDefinitionPersistence));
        Assert.All(entity.GetProperties(), property => Assert.False(property.IsConcurrencyToken));
        Assert.Empty(entity.GetIndexes());
    }

    private static void AssertGuidProperty(IReadOnlyEntityType entity, string propertyName)
    {
        var property = AssertProperty(entity, propertyName, typeof(Guid), "uniqueidentifier", false);
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
    }

    private static IReadOnlyProperty AssertProperty(
        IReadOnlyEntityType entity,
        string propertyName,
        Type clrType,
        string columnType,
        bool nullable)
    {
        var property = entity.FindProperty(propertyName)!;
        Assert.Equal(clrType, property.ClrType);
        Assert.Equal(columnType, property.GetColumnType());
        Assert.Equal(nullable, property.IsNullable);
        return property;
    }

    private static MyShopDbContext CreateContext() => new(
        new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options);
}
