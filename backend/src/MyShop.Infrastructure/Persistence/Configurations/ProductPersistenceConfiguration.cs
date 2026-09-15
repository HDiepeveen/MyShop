using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class ProductPersistenceConfiguration : IEntityTypeConfiguration<ProductPersistence>
{
    public void Configure(EntityTypeBuilder<ProductPersistence> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.Property(product => product.ProductTypeId).IsRequired();
        builder.Property(product => product.Name).IsRequired();
        builder.Property(product => product.Version)
            .IsRequired()
            .HasColumnType("uniqueidentifier")
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasOne(product => product.ProductType)
            .WithMany()
            .HasForeignKey(product => product.ProductTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(product => product.ProductTypeId);
    }
}
