using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HDWebFramework.Infrastructure.Persistence.Models;

namespace HDWebFramework.Infrastructure.Persistence.Configurations;

internal sealed class ProductAttributeMultiChoiceValuePersistenceConfiguration
    : IEntityTypeConfiguration<ProductAttributeMultiChoiceValuePersistence>
{
    public void Configure(EntityTypeBuilder<ProductAttributeMultiChoiceValuePersistence> builder)
    {
        builder.ToTable("ProductAttributeMultiChoiceValues");
        builder.HasKey(value => new { value.ProductId, value.AttributeDefinitionId, value.Ordinal });
        builder.Property(value => value.ProductId).IsRequired().ValueGeneratedNever();
        builder.Property(value => value.AttributeDefinitionId).IsRequired().ValueGeneratedNever();
        builder.Property(value => value.Ordinal).IsRequired().HasColumnType("int");
        builder.Property(value => value.Value).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(value => value.AttributeValue)
            .WithMany(value => value.MultiChoiceValues)
            .HasForeignKey(value => new { value.ProductId, value.AttributeDefinitionId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
