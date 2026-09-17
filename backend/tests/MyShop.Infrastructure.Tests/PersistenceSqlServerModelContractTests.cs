using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests;

public sealed class PersistenceSqlServerModelContractTests
{
    private static readonly Type[] Models = typeof(ProductPersistence).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(ProductPersistence).Namespace
            && type.Name.EndsWith("Persistence", StringComparison.Ordinal))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    private static readonly IReadOnlyDictionary<Type, string> ExpectedTables = new Dictionary<Type, string>
    {
        [typeof(AttributeDefinitionPersistence)] = "AttributeDefinitions",
        [typeof(CategoryPersistence)] = "Categories",
        [typeof(ProductPersistence)] = "Products",
        [typeof(ProductAttributeValuePersistence)] = "ProductAttributeValues",
        [typeof(ProductAttributeMultiChoiceValuePersistence)] = "ProductAttributeMultiChoiceValues",
        [typeof(ProductCategoryPersistence)] = "ProductCategories",
        [typeof(ProductTypePersistence)] = "ProductTypes",
        [typeof(ProductVariantPersistence)] = "ProductVariants",
        [typeof(ProductVariantAttributeValuePersistence)] = "ProductVariantAttributeValues",
        [typeof(ProductVariantAttributeMultiChoiceValuePersistence)] = "ProductVariantAttributeMultiChoiceValues"
        , [typeof(PriceRulePersistence)] = "PriceRules"
    };

    public static TheoryData<Type> ModelData => new(Models);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_UsesExpectedTable(Type modelType) =>
        Assert.Equal(ExpectedTables[modelType], Entity(modelType).GetTableName());

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_UsesExpectedClrType(Type modelType) =>
        Assert.Equal(modelType, Entity(modelType).ClrType);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_HasPrimaryKey(Type modelType) =>
        Assert.NotNull(Entity(modelType).FindPrimaryKey());

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_PrimaryKeyHasProperties(Type modelType) =>
        Assert.NotEmpty(Entity(modelType).FindPrimaryKey()!.Properties);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_PrimaryKeyPropertiesAreRequired(Type modelType) =>
        Assert.All(Entity(modelType).FindPrimaryKey()!.Properties, property => Assert.False(property.IsNullable));

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_HasMappedProperties(Type modelType) =>
        Assert.NotEmpty(Entity(modelType).GetProperties());

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_TableNameIsPlural(Type modelType) =>
        Assert.EndsWith("s", Entity(modelType).GetTableName(), StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_DeclaresPrimaryKey(Type modelType) =>
        Assert.NotNull(Entity(modelType).FindPrimaryKey());

    [Theory]
    [MemberData(nameof(ModelData))]
    public void SqlServerModel_HasNoDefiningQuery(Type modelType) =>
        Assert.Null(Entity(modelType).GetViewName());

    [Fact]
    public void SqlServerModel_ContainsExpectedEntityCount() => Assert.Equal(11, Model().GetEntityTypes().Count());

    [Fact]
    public void SqlServerModel_ContainsExpectedClrTypes() =>
        Assert.Equal(Models.OrderBy(type => type.FullName), Model().GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.FullName));

    [Fact]
    public void SqlServerModel_ContainsUniqueTableNames() =>
        Assert.Equal(11, Model().GetEntityTypes().Select(entity => entity.GetTableName()).Distinct().Count());

    [Fact]
    public void SqlServerModel_ContainsOnlyExpectedTables() =>
        Assert.Equal(ExpectedTables.Values.OrderBy(name => name), Model().GetEntityTypes().Select(entity => entity.GetTableName()).OrderBy(name => name));

    [Fact]
    public void SqlServerModel_AllEntitiesHavePrimaryKeys() =>
        Assert.All(Model().GetEntityTypes(), entity => Assert.NotNull(entity.FindPrimaryKey()));

    [Fact]
    public void SqlServerModel_AllKeysAreNonNullable() =>
        Assert.All(Model().GetEntityTypes(), entity =>
            Assert.All(entity.FindPrimaryKey()!.Properties, property => Assert.False(property.IsNullable)));

    [Fact]
    public void SqlServerModel_AllTablesUsePluralNames() =>
        Assert.All(Model().GetEntityTypes(), entity => Assert.EndsWith("s", entity.GetTableName(), StringComparison.Ordinal));

    [Fact]
    public void SqlServerModel_AllEntitiesAreConcrete() =>
        Assert.All(Model().GetEntityTypes(), entity => Assert.False(entity.ClrType.IsAbstract));

    [Fact]
    public void SqlServerModel_AllEntitiesHaveProperties() =>
        Assert.All(Model().GetEntityTypes(), entity => Assert.NotEmpty(entity.GetProperties()));

    [Fact]
    public void SqlServerModel_UsesSqlServerProvider()
    {
        using var context = CreateContext();
        Assert.Contains("SqlServer", context.Database.ProviderName, StringComparison.OrdinalIgnoreCase);
    }

    private static IEntityType Entity(Type modelType) =>
        Model().FindEntityType(modelType)!;

    private static IModel Model()
    {
        using var context = CreateContext();
        return context.Model;
    }

    private static MyShopDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MyShop.ContractTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
}
