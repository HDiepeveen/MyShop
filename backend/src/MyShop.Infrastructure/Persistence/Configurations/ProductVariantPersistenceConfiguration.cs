using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantPersistenceConfiguration : IEntityTypeConfiguration<ProductVariantPersistence>
{
    public void Configure(EntityTypeBuilder<ProductVariantPersistence> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.Id).ValueGeneratedNever();
        builder.Property(variant => variant.ProductId).IsRequired();
        builder.Property(variant => variant.Name).IsRequired();
        builder.Property(variant => variant.Sku)
            .IsRequired(false)
            .HasMaxLength(64)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(variant => variant.Ordinal).IsRequired();

        builder.HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(variant => variant.ProductId);
        builder.HasIndex(variant => variant.Sku)
            .IsUnique()
            .HasDatabaseName("UX_ProductVariants_Sku")
            .HasFilter("[Sku] IS NOT NULL");
    }
}
