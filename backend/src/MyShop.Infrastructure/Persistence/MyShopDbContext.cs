using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence;

public sealed class MyShopDbContext(DbContextOptions<MyShopDbContext> options) : DbContext(options)
{
    internal DbSet<ProductTypePersistence> ProductTypes => Set<ProductTypePersistence>();
    internal DbSet<AttributeDefinitionPersistence> AttributeDefinitions => Set<AttributeDefinitionPersistence>();
    internal DbSet<CategoryPersistence> Categories => Set<CategoryPersistence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyShopDbContext).Assembly);
    }
}