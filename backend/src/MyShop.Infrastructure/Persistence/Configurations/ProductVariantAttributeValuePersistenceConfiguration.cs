using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantAttributeValuePersistenceConfiguration
    : IEntityTypeConfiguration<ProductVariantAttributeValuePersistence>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttributeValuePersistence> builder)
    {
        builder.ToTable("ProductVariantAttributeValues");
        builder.HasKey(value => new { value.ProductVariantId, value.AttributeDefinitionId });
        builder.Property(value => value.ProductVariantId).IsRequired().ValueGeneratedNever();
        builder.Property(value => value.AttributeDefinitionId).IsRequired().ValueGeneratedNever();
        builder.Property(value => value.DataType).IsRequired().HasConversion<int>().HasColumnType("int");
        builder.Property(value => value.Ordinal).IsRequired().HasColumnType("int");
        builder.Property(value => value.TextValue).IsRequired(false).HasColumnType("nvarchar(max)");
        builder.Property(value => value.IntegerValue).IsRequired(false).HasColumnType("bigint");
        builder.Property(value => value.DecimalCoefficient)
            .IsRequired(false)
            .HasPrecision(29, 0);
        builder.Property(value => value.DecimalScale).IsRequired(false).HasColumnType("tinyint");
        builder.Property(value => value.BooleanValue).IsRequired(false).HasColumnType("bit");
        builder.Property(value => value.DateValue).IsRequired(false).HasColumnType("date");
        builder.Property(value => value.ChoiceValue).IsRequired(false).HasColumnType("nvarchar(max)");

        builder.HasOne(value => value.ProductVariant)
            .WithMany(variant => variant.AttributeValues)
            .HasForeignKey(value => value.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
