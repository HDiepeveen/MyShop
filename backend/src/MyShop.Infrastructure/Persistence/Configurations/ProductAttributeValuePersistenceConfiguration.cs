using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class ProductAttributeValuePersistenceConfiguration
    : IEntityTypeConfiguration<ProductAttributeValuePersistence>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValuePersistence> builder)
    {
        builder.ToTable("ProductAttributeValues");
        builder.HasKey(value => new { value.ProductId, value.AttributeDefinitionId });
        builder.Property(value => value.ProductId).IsRequired().ValueGeneratedNever();
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

        builder.HasOne(value => value.Product)
            .WithMany(product => product.AttributeValues)
            .HasForeignKey(value => value.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
