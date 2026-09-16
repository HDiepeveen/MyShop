using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantAttributeMultiChoiceValuePersistenceConfiguration
    : IEntityTypeConfiguration<ProductVariantAttributeMultiChoiceValuePersistence>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttributeMultiChoiceValuePersistence> builder)
    {
        builder.ToTable("ProductVariantAttributeMultiChoiceValues");
        builder.HasKey(value => new { value.ProductVariantId, value.AttributeDefinitionId, value.Ordinal });
        builder.Property(value => value.ProductVariantId).IsRequired().ValueGeneratedNever();
        builder.Property(value => value.AttributeDefinitionId).IsRequired().ValueGeneratedNever();
        builder.Property(value => value.Ordinal).IsRequired().HasColumnType("int");
        builder.Property(value => value.Value).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(value => value.AttributeValue)
            .WithMany(value => value.MultiChoiceValues)
            .HasForeignKey(value => new { value.ProductVariantId, value.AttributeDefinitionId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
