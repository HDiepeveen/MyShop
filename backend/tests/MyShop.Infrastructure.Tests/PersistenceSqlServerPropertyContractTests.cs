using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests;

public sealed class PersistenceSqlServerPropertyContractTests
{
    private static readonly Type[] Models = typeof(ProductPersistence).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(ProductPersistence).Namespace && type.Name.EndsWith("Persistence", StringComparison.Ordinal))
        .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();

    public static TheoryData<Type> ModelData => new(Models);

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_HaveNames(Type modelType) => Assert.All(Entity(modelType).GetProperties(), p => Assert.False(string.IsNullOrWhiteSpace(p.Name)));

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_HaveClrTypes(Type modelType) => Assert.All(Entity(modelType).GetProperties(), p => Assert.NotEqual(typeof(object), p.ClrType));

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_AreNotShadowProperties(Type modelType) => Assert.All(Entity(modelType).GetProperties(), p => Assert.NotNull(p.PropertyInfo));

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_HaveColumnNames(Type modelType) => Assert.All(Entity(modelType).GetProperties(), p => Assert.False(string.IsNullOrWhiteSpace(p.GetColumnName())));

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_HaveContainingEntity(Type modelType) => Assert.All(Entity(modelType).GetProperties(), p => Assert.Equal(modelType, p.DeclaringType.ClrType));

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_AreNotIndexerProperties(Type modelType) => Assert.All(Entity(modelType).GetProperties(), p => Assert.Null(p.PropertyInfo!.GetIndexParameters().Length == 0 ? null : p.PropertyInfo));

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_HaveUniqueNames(Type modelType) { var ps = Entity(modelType).GetProperties().ToArray(); Assert.Equal(ps.Length, ps.Select(p => p.Name).Distinct().Count()); }

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_HaveUniqueColumns(Type modelType) { var ps = Entity(modelType).GetProperties().ToArray(); Assert.Equal(ps.Length, ps.Select(p => p.GetColumnName()).Distinct().Count()); }

    [Theory, MemberData(nameof(ModelData))]
    public void Properties_AreMappedToSqlServer(Type modelType) => Assert.All(Entity(modelType).GetProperties(), p => Assert.NotNull(p.DeclaringType.GetTableName()));

    [Fact]
    public void PropertyModel_HasTenEntities() => Assert.Equal(10, Model().GetEntityTypes().Count());

    [Fact]
    public void PropertyModel_HasNoShadowProperties() => Assert.All(Model().GetEntityTypes(), e => Assert.All(e.GetProperties(), p => Assert.NotNull(p.PropertyInfo)));

    [Fact]
    public void PropertyModel_HasNoDuplicatePropertyNames() => Assert.All(Model().GetEntityTypes(), e => { var ps = e.GetProperties().ToArray(); Assert.Equal(ps.Length, ps.Select(p => p.Name).Distinct().Count()); });

    [Fact]
    public void PropertyModel_HasNoDuplicateColumnNames() => Assert.All(Model().GetEntityTypes(), e => { var ps = e.GetProperties().ToArray(); Assert.Equal(ps.Length, ps.Select(p => p.GetColumnName()).Distinct().Count()); });

    [Fact]
    public void PropertyModel_AllPropertiesHaveColumns() => Assert.All(Model().GetEntityTypes(), e => Assert.All(e.GetProperties(), p => Assert.False(string.IsNullOrWhiteSpace(p.GetColumnName()))));

    [Fact]
    public void PropertyModel_AllPropertiesHaveClrTypes() => Assert.All(Model().GetEntityTypes(), e => Assert.All(e.GetProperties(), p => Assert.NotNull(p.ClrType)));

    [Fact]
    public void PropertyModel_AllPropertiesBelongToEntity() => Assert.All(Model().GetEntityTypes(), e => Assert.All(e.GetProperties(), p => Assert.Equal(e, p.DeclaringType)));

    [Fact]
    public void PropertyModel_AllEntitiesHaveProperties() => Assert.All(Model().GetEntityTypes(), e => Assert.NotEmpty(e.GetProperties()));

    [Fact]
    public void PropertyModel_UsesExpectedEntityTypes() => Assert.Equal(Models.OrderBy(t => t.FullName), Model().GetEntityTypes().Select(e => e.ClrType).OrderBy(t => t.FullName));

    [Fact]
    public void PropertyModel_UsesSqlServerProvider() { using var c = CreateContext(); Assert.Contains("SqlServer", c.Database.ProviderName, StringComparison.OrdinalIgnoreCase); }

    [Fact]
    public void PropertyModel_IsBuiltWithoutConnection() { using var c = CreateContext(); Assert.NotNull(c.Model); }

    private static IEntityType Entity(Type type) => Model().FindEntityType(type)!;
    private static IModel Model() { using var c = CreateContext(); return c.Model; }
    private static MyShopDbContext CreateContext() => new(new DbContextOptionsBuilder<MyShopDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MyShop.ContractTests;Trusted_Connection=True;TrustServerCertificate=True").Options);
}
