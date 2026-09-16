using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence;

public sealed class ProductCategoryPersistenceConfigurationTests
{
    [Fact]
    public void ProductCategory_MapsTableCompositeKeyAndRequiredProperties()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductCategoryPersistence))!;

        Assert.Equal("ProductCategories", entity.GetTableName());
        Assert.Equal(
            ["ProductId", "CategoryId"],
            entity.FindPrimaryKey()!.Properties.Select(property => property.Name));

        var productId = entity.FindProperty("ProductId")!;
        Assert.False(productId.IsNullable);
        Assert.Equal(typeof(Guid), productId.ClrType);
        Assert.Equal("uniqueidentifier", productId.GetColumnType());
        Assert.Equal(ValueGenerated.Never, productId.ValueGenerated);

        var categoryId = entity.FindProperty("CategoryId")!;
        Assert.False(categoryId.IsNullable);
        Assert.Equal(typeof(Guid), categoryId.ClrType);
        Assert.Equal("uniqueidentifier", categoryId.GetColumnType());
        Assert.Equal(ValueGenerated.Never, categoryId.ValueGenerated);

        var ordinal = entity.FindProperty("Ordinal")!;
        Assert.False(ordinal.IsNullable);
        Assert.Equal(typeof(int), ordinal.ClrType);
        Assert.Equal("int", ordinal.GetColumnType());
    }

    [Fact]
    public void ProductCategory_HasSingleRequiredProductRelationshipWithCascade()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductCategoryPersistence))!;
        var relationship = Assert.Single(entity.GetForeignKeys());

        Assert.Equal("ProductId", Assert.Single(relationship.Properties).Name);
        Assert.Equal(typeof(ProductPersistence), relationship.PrincipalEntityType.ClrType);
        Assert.True(relationship.IsRequired);
        Assert.False(relationship.IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior);
        Assert.Equal("Product", relationship.DependentToPrincipal!.Name);
        Assert.Equal("Categories", relationship.PrincipalToDependent!.Name);
    }

    [Fact]
    public void ProductCategory_HasNoCategoryRelationshipOrNavigation()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductCategoryPersistence))!;

        Assert.DoesNotContain(entity.GetForeignKeys(), relationship =>
            relationship.Properties.Any(property => property.Name == "CategoryId"));
        Assert.DoesNotContain(entity.GetForeignKeys(), relationship =>
            relationship.PrincipalEntityType.ClrType == typeof(CategoryPersistence));
        Assert.DoesNotContain(entity.GetNavigations(), navigation =>
            navigation.TargetEntityType.ClrType == typeof(CategoryPersistence));
        Assert.Null(typeof(ProductCategoryPersistence).GetProperty("Category"));
    }

    [Fact]
    public void ProductCategory_HasNoSpeculativeOrRedundantIndexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductCategoryPersistence))!;

        Assert.Empty(entity.GetIndexes());
    }

    private static MyShopDbContext CreateContext() => new(
        new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options);
}
