using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

    public static TheoryData<Type, string, Type> PropertyClrTypes => new()
    {
        { typeof(ProductPersistence), "Id", typeof(Guid) },
        { typeof(ProductPersistence), "ProductTypeId", typeof(Guid) },
        { typeof(ProductPersistence), "Name", typeof(string) },
        { typeof(ProductPersistence), "Version", typeof(Guid) },
        { typeof(ProductVariantPersistence), "Id", typeof(Guid) },
        { typeof(ProductVariantPersistence), "ProductId", typeof(Guid) },
        { typeof(ProductVariantPersistence), "Name", typeof(string) },
        { typeof(ProductVariantPersistence), "Sku", typeof(string) },
        { typeof(ProductVariantPersistence), "Ordinal", typeof(int) },
        { typeof(ProductTypePersistence), "Id", typeof(Guid) },
        { typeof(ProductTypePersistence), "Name", typeof(string) },
        { typeof(AttributeDefinitionPersistence), "Id", typeof(Guid) },
        { typeof(AttributeDefinitionPersistence), "ProductTypeId", typeof(Guid) },
        { typeof(AttributeDefinitionPersistence), "Code", typeof(string) },
        { typeof(AttributeDefinitionPersistence), "DisplayName", typeof(string) },
        { typeof(AttributeDefinitionPersistence), "IsRequired", typeof(bool) },
        { typeof(AttributeDefinitionPersistence), "IsFilterable", typeof(bool) },
        { typeof(CategoryPersistence), "Id", typeof(Guid) },
        { typeof(CategoryPersistence), "Name", typeof(string) },
        { typeof(CategoryPersistence), "ParentCategoryId", typeof(Guid?) },
        { typeof(ProductCategoryPersistence), "ProductId", typeof(Guid) },
        { typeof(ProductCategoryPersistence), "CategoryId", typeof(Guid) },
        { typeof(ProductCategoryPersistence), "Ordinal", typeof(int) },
        { typeof(ProductAttributeValuePersistence), "IntegerValue", typeof(long?) },
        { typeof(ProductAttributeValuePersistence), "DecimalCoefficient", typeof(decimal?) },
        { typeof(ProductAttributeValuePersistence), "DecimalScale", typeof(byte?) },
        { typeof(ProductAttributeValuePersistence), "BooleanValue", typeof(bool?) },
        { typeof(ProductAttributeValuePersistence), "DateValue", typeof(DateOnly?) },
        { typeof(ProductVariantAttributeValuePersistence), "IntegerValue", typeof(long?) },
        { typeof(ProductVariantAttributeValuePersistence), "DateValue", typeof(DateOnly?) }
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

    public static TheoryData<Type, Type, string[]> ForeignKeyProperties => new()
    {
        { typeof(ProductPersistence), typeof(ProductTypePersistence), ["ProductTypeId"] },
        { typeof(ProductVariantPersistence), typeof(ProductPersistence), ["ProductId"] },
        { typeof(CategoryPersistence), typeof(CategoryPersistence), ["ParentCategoryId"] },
        { typeof(AttributeDefinitionPersistence), typeof(ProductTypePersistence), ["ProductTypeId"] },
        { typeof(ProductCategoryPersistence), typeof(ProductPersistence), ["ProductId"] },
        { typeof(ProductAttributeValuePersistence), typeof(ProductPersistence), ["ProductId"] },
        { typeof(ProductAttributeMultiChoiceValuePersistence), typeof(ProductAttributeValuePersistence), ["ProductId", "AttributeDefinitionId"] },
        { typeof(ProductVariantAttributeValuePersistence), typeof(ProductVariantPersistence), ["ProductVariantId"] },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), typeof(ProductVariantAttributeValuePersistence), ["ProductVariantId", "AttributeDefinitionId"] }
    };

    public static TheoryData<Type, Type, string, string?> NavigationMappings => new()
    {
        { typeof(ProductPersistence), typeof(ProductTypePersistence), "ProductType", null },
        { typeof(ProductVariantPersistence), typeof(ProductPersistence), "Product", "Variants" },
        { typeof(CategoryPersistence), typeof(CategoryPersistence), "Parent", "Children" },
        { typeof(AttributeDefinitionPersistence), typeof(ProductTypePersistence), "ProductType", "AttributeDefinitions" },
        { typeof(ProductCategoryPersistence), typeof(ProductPersistence), "Product", "Categories" },
        { typeof(ProductAttributeValuePersistence), typeof(ProductPersistence), "Product", "AttributeValues" },
        { typeof(ProductAttributeMultiChoiceValuePersistence), typeof(ProductAttributeValuePersistence), "AttributeValue", "MultiChoiceValues" },
        { typeof(ProductVariantAttributeValuePersistence), typeof(ProductVariantPersistence), "ProductVariant", "AttributeValues" },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), typeof(ProductVariantAttributeValuePersistence), "AttributeValue", "MultiChoiceValues" }
    };

    public static TheoryData<Type, Type, bool> RelationshipRequiredness => new()
    {
        { typeof(ProductPersistence), typeof(ProductTypePersistence), true },
        { typeof(ProductVariantPersistence), typeof(ProductPersistence), true },
        { typeof(CategoryPersistence), typeof(CategoryPersistence), false },
        { typeof(AttributeDefinitionPersistence), typeof(ProductTypePersistence), true },
        { typeof(ProductCategoryPersistence), typeof(ProductPersistence), true },
        { typeof(ProductAttributeValuePersistence), typeof(ProductPersistence), true },
        { typeof(ProductAttributeMultiChoiceValuePersistence), typeof(ProductAttributeValuePersistence), true },
        { typeof(ProductVariantAttributeValuePersistence), typeof(ProductVariantPersistence), true },
        { typeof(ProductVariantAttributeMultiChoiceValuePersistence), typeof(ProductVariantAttributeValuePersistence), true }
    };

    public static TheoryData<Type> EntitiesWithoutShadowProperties => new()
    {
        typeof(ProductPersistence), typeof(ProductVariantPersistence), typeof(ProductCategoryPersistence),
        typeof(ProductAttributeValuePersistence), typeof(ProductAttributeMultiChoiceValuePersistence),
        typeof(ProductVariantAttributeValuePersistence), typeof(ProductVariantAttributeMultiChoiceValuePersistence),
        typeof(ProductTypePersistence), typeof(AttributeDefinitionPersistence), typeof(CategoryPersistence)
    };

    public static TheoryData<Type, string[], bool, string?> IndexMappings => new()
    {
        { typeof(ProductPersistence), ["ProductTypeId"], false, null },
        { typeof(ProductVariantPersistence), ["ProductId"], false, null },
        { typeof(ProductVariantPersistence), ["Sku"], true, "[Sku] IS NOT NULL" },
        { typeof(AttributeDefinitionPersistence), ["ProductTypeId", "Code"], true, null }
    };

    public static TheoryData<Type, string, int> MaxLengths => new()
    {
        { typeof(ProductVariantPersistence), "Sku", 64 },
        { typeof(AttributeDefinitionPersistence), "Code", 64 }
    };

    public static TheoryData<Type, string> DecimalProperties => new()
    {
        { typeof(ProductAttributeValuePersistence), "DecimalCoefficient" },
        { typeof(ProductVariantAttributeValuePersistence), "DecimalCoefficient" }
    };

    public static TheoryData<Type, string, string> UniqueIdentifierColumns => new()
    {
        { typeof(ProductPersistence), "Version", "uniqueidentifier" },
        { typeof(ProductCategoryPersistence), "ProductId", "uniqueidentifier" },
        { typeof(ProductCategoryPersistence), "CategoryId", "uniqueidentifier" }
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
    [MemberData(nameof(PropertyClrTypes))]
    public void Property_UsesExpectedClrType(Type entityType, string propertyName, Type clrType)
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;
        Assert.Equal(clrType, property.ClrType);
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
    [MemberData(nameof(ForeignKeyProperties))]
    public void Relationship_UsesExpectedForeignKey(
        Type dependentType, Type principalType, string[] propertyNames)
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(dependentType)!;
        var foreignKey = Assert.Single(entity.GetForeignKeys(),
            candidate => candidate.PrincipalEntityType.ClrType == principalType);
        Assert.Equal(propertyNames, foreignKey.Properties.Select(property => property.Name));
    }

    [Theory]
    [MemberData(nameof(NavigationMappings))]
    public void Relationship_UsesExpectedNavigations(
        Type dependentType, Type principalType, string dependentNavigation, string? principalNavigation)
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(dependentType)!;
        var foreignKey = Assert.Single(entity.GetForeignKeys(),
            candidate => candidate.PrincipalEntityType.ClrType == principalType);
        Assert.Equal(dependentNavigation, foreignKey.DependentToPrincipal!.Name);
        Assert.Equal(principalNavigation, foreignKey.PrincipalToDependent?.Name);
    }

    [Theory]
    [MemberData(nameof(RelationshipRequiredness))]
    public void Relationship_UsesExpectedRequiredness(Type dependentType, Type principalType, bool required)
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(dependentType)!;
        var foreignKey = Assert.Single(entity.GetForeignKeys(),
            candidate => candidate.PrincipalEntityType.ClrType == principalType);
        Assert.Equal(required, foreignKey.IsRequired);
    }

    [Theory]
    [MemberData(nameof(EntitiesWithoutShadowProperties))]
    public void Entity_HasNoShadowProperties(Type entityType)
    {
        using var context = CreateContext();
        var properties = context.Model.FindEntityType(entityType)!.GetProperties();
        Assert.DoesNotContain(properties, property => property.IsShadowProperty());
    }

    [Theory]
    [MemberData(nameof(IndexMappings))]
    public void Entity_UsesExpectedIndex(Type entityType, string[] propertyNames, bool unique, string? filter)
    {
        using var context = CreateContext();
        var indexes = context.Model.FindEntityType(entityType)!.GetIndexes();
        var index = Assert.Single(indexes,
            candidate => candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
        Assert.Equal(unique, index.IsUnique);
        Assert.Equal(filter, index.GetFilter());
    }

    [Theory]
    [MemberData(nameof(MaxLengths))]
    public void Property_UsesExpectedMaximumLength(Type entityType, string propertyName, int maxLength)
    {
        using var context = CreateContext();
        Assert.Equal(maxLength, context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!.GetMaxLength());
    }

    [Theory]
    [MemberData(nameof(DecimalProperties))]
    public void DecimalCoefficient_UsesExactIntegerPrecision(Type entityType, string propertyName)
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;
        Assert.Equal(29, property.GetPrecision());
        Assert.Equal(0, property.GetScale());
    }

    [Theory]
    [MemberData(nameof(UniqueIdentifierColumns))]
    public void Property_UsesExplicitUniqueIdentifierColumn(Type entityType, string propertyName, string columnType)
    {
        using var context = CreateContext();
        Assert.Equal(columnType, context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!.GetColumnType());
    }

    [Fact]
    public void ProductVersion_IsConcurrencyToken()
    {
        using var context = CreateContext();
        Assert.True(context.Model.FindEntityType(typeof(ProductPersistence))!
            .FindProperty(nameof(ProductPersistence.Version))!.IsConcurrencyToken);
    }

    [Fact]
    public void VariantSku_UsesBinarySqlServerCollation()
    {
        using var context = CreateContext();
        Assert.Equal("Latin1_General_100_BIN2", context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(ProductVariantPersistence))!
            .FindProperty(nameof(ProductVariantPersistence.Sku))!.GetCollation());
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
