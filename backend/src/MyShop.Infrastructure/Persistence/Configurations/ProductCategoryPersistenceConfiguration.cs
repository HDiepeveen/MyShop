using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class ProductCategoryPersistenceConfiguration
    : IEntityTypeConfiguration<ProductCategoryPersistence>
{
    public void Configure(EntityTypeBuilder<ProductCategoryPersistence> builder)
    {
        builder.ToTable("ProductCategories");
        builder.HasKey(productCategory => new { productCategory.ProductId, productCategory.CategoryId });
        builder.Property(productCategory => productCategory.ProductId)
            .IsRequired()
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(productCategory => productCategory.CategoryId)
            .IsRequired()
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(productCategory => productCategory.Ordinal)
            .IsRequired()
            .HasColumnType("int");

        builder.HasOne(productCategory => productCategory.Product)
            .WithMany(product => product.Categories)
            .HasForeignKey(productCategory => productCategory.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
