using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class ProductImagePersistenceConfiguration : IEntityTypeConfiguration<ProductImagePersistence>
{
    public void Configure(EntityTypeBuilder<ProductImagePersistence> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(image => image.Id);
        builder.Property(image => image.ContentType).HasMaxLength(32).IsRequired();
        builder.Property(image => image.AlternativeText).HasMaxLength(250).IsRequired();
        builder.Property(image => image.FileName).HasMaxLength(120).IsRequired();
        builder.Property(image => image.Bytes).IsRequired();
        builder.HasIndex(image => new { image.ProductId, image.Ordinal });
        builder.HasOne(image => image.Product).WithMany(product => product.Images).HasForeignKey(image => image.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
