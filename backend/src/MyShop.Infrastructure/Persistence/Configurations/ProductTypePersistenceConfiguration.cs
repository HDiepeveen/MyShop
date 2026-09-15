using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class ProductTypePersistenceConfiguration : IEntityTypeConfiguration<ProductTypePersistence>
{
    public void Configure(EntityTypeBuilder<ProductTypePersistence> builder)
    {
        builder.ToTable("ProductTypes");
        builder.HasKey(productType => productType.Id);
        builder.Property(productType => productType.Id).ValueGeneratedNever();
        builder.Property(productType => productType.Name).IsRequired();

        builder.HasMany(productType => productType.AttributeDefinitions)
            .WithOne(attribute => attribute.ProductType)
            .HasForeignKey(attribute => attribute.ProductTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}