using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence;

public sealed class MyShopDbContext(DbContextOptions<MyShopDbContext> options) : DbContext(options)
{
    internal DbSet<ProductTypePersistence> ProductTypes => Set<ProductTypePersistence>();
    internal DbSet<AttributeDefinitionPersistence> AttributeDefinitions => Set<AttributeDefinitionPersistence>();
    internal DbSet<CategoryPersistence> Categories => Set<CategoryPersistence>();
    internal DbSet<ProductPersistence> Products => Set<ProductPersistence>();
    internal DbSet<ProductVariantPersistence> ProductVariants => Set<ProductVariantPersistence>();
    internal DbSet<ProductCategoryPersistence> ProductCategories => Set<ProductCategoryPersistence>();
    internal DbSet<ProductAttributeValuePersistence> ProductAttributeValues => Set<ProductAttributeValuePersistence>();
    internal DbSet<ProductVariantAttributeValuePersistence> ProductVariantAttributeValues => Set<ProductVariantAttributeValuePersistence>();
    internal DbSet<ProductAttributeMultiChoiceValuePersistence> ProductAttributeMultiChoiceValues => Set<ProductAttributeMultiChoiceValuePersistence>();
    internal DbSet<ProductVariantAttributeMultiChoiceValuePersistence> ProductVariantAttributeMultiChoiceValues => Set<ProductVariantAttributeMultiChoiceValuePersistence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyShopDbContext).Assembly);
    }
}
