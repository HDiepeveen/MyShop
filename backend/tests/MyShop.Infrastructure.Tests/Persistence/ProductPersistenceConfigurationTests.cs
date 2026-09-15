using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence;

public sealed class ProductPersistenceConfigurationTests
{
    [Fact]
    public void Model_ContainsExactlyFivePersistenceEntities()
    {
        using var context = CreateContext();
        var types = context.Model.GetEntityTypes().Select(entity => entity.ClrType).ToHashSet();

        Assert.Equal(5, types.Count);
        Assert.True(types.SetEquals([
            typeof(ProductTypePersistence), typeof(AttributeDefinitionPersistence),
            typeof(CategoryPersistence), typeof(ProductPersistence), typeof(ProductVariantPersistence)]));
        Assert.DoesNotContain(types, type => type.Assembly == typeof(MyShop.Domain.Catalog.Product).Assembly);
    }

    [Fact]
    public void Product_MapsTableKeyAndRequiredProperties()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductPersistence))!;

        Assert.Equal("Products", entity.GetTableName());
        Assert.Equal("Id", Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
        var id = entity.FindProperty("Id")!;
        Assert.False(id.IsNullable);
        Assert.Equal("uniqueidentifier", id.GetColumnType());
        Assert.Equal(ValueGenerated.Never, id.ValueGenerated);
        Assert.False(entity.FindProperty("ProductTypeId")!.IsNullable);
        var name = entity.FindProperty("Name")!;
        Assert.False(name.IsNullable);
        Assert.Null(name.GetMaxLength());
        Assert.Equal("nvarchar(max)", name.GetColumnType());
    }

    [Fact]
    public void Product_VersionIsRequiredApplicationManagedGuidConcurrencyToken()
    {
        using var context = CreateContext();
        var version = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(ProductPersistence))!.FindProperty("Version")!;

        Assert.False(version.IsNullable);
        Assert.Equal(typeof(Guid), version.ClrType);
        Assert.Equal("uniqueidentifier", version.GetColumnType());
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.Never, version.ValueGenerated);
        Assert.Null(version.GetDefaultValueSql());
        Assert.Null(version.GetComputedColumnSql());
        Assert.Null(version.FindAnnotation(RelationalAnnotationNames.DefaultValue));
    }

    [Fact]
    public void Product_HasSingleRequiredProductTypeRelationshipWithoutCascade()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductPersistence))!;
        var relationship = Assert.Single(entity.GetForeignKeys());

        Assert.Equal("ProductTypeId", Assert.Single(relationship.Properties).Name);
        Assert.Equal(typeof(ProductTypePersistence), relationship.PrincipalEntityType.ClrType);
        Assert.True(relationship.IsRequired);
        Assert.False(relationship.IsUnique);
        Assert.Equal(DeleteBehavior.NoAction, relationship.DeleteBehavior);
        Assert.Equal("ProductType", relationship.DependentToPrincipal!.Name);
        Assert.Null(relationship.PrincipalToDependent);
        var index = Assert.Single(entity.GetIndexes());
        Assert.Equal("ProductTypeId", Assert.Single(index.Properties).Name);
        Assert.False(index.IsUnique);
    }

    [Fact]
    public void Variant_MapsTableKeyAndRequiredProperties()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductVariantPersistence))!;

        Assert.Equal("ProductVariants", entity.GetTableName());
        Assert.Equal("Id", Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
        var id = entity.FindProperty("Id")!;
        Assert.False(id.IsNullable);
        Assert.Equal("uniqueidentifier", id.GetColumnType());
        Assert.Equal(ValueGenerated.Never, id.ValueGenerated);
        Assert.False(entity.FindProperty("ProductId")!.IsNullable);
        var name = entity.FindProperty("Name")!;
        Assert.False(name.IsNullable);
        Assert.Null(name.GetMaxLength());
        Assert.Equal("nvarchar(max)", name.GetColumnType());
        var ordinal = entity.FindProperty("Ordinal")!;
        Assert.False(ordinal.IsNullable);
        Assert.Equal("int", ordinal.GetColumnType());
    }

    [Fact]
    public void Variant_HasSingleRequiredProductRelationshipWithCascade()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductVariantPersistence))!;
        var relationship = Assert.Single(entity.GetForeignKeys());

        Assert.Equal("ProductId", Assert.Single(relationship.Properties).Name);
        Assert.Equal(typeof(ProductPersistence), relationship.PrincipalEntityType.ClrType);
        Assert.True(relationship.IsRequired);
        Assert.False(relationship.IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior);
        Assert.Equal("Product", relationship.DependentToPrincipal!.Name);
        Assert.Equal("Variants", relationship.PrincipalToDependent!.Name);
        Assert.Equal(2, entity.GetIndexes().Count());
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Count == 1 && candidate.Properties[0].Name == "ProductId");
        Assert.False(index.IsUnique);
    }

    [Fact]
    public void Variant_SkuHasNullableBoundedBinaryCollationAndFilteredUniqueIndex()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(ProductVariantPersistence))!;
        var sku = entity.FindProperty("Sku")!;

        Assert.True(sku.IsNullable);
        Assert.Equal(64, sku.GetMaxLength());
        Assert.Equal("nvarchar(64)", sku.GetColumnType());
        Assert.Equal("Latin1_General_100_BIN2", sku.GetCollation());
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Count == 1 && candidate.Properties[0].Name == "Sku");
        Assert.True(index.IsUnique);
        Assert.Equal("UX_ProductVariants_Sku", index.GetDatabaseName());
        Assert.Equal("[Sku] IS NOT NULL", index.GetFilter());
    }

    private static MyShopDbContext CreateContext() => new(
        new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options);
}
