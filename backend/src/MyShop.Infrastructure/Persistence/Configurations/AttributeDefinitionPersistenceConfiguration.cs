using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class AttributeDefinitionPersistenceConfiguration : IEntityTypeConfiguration<AttributeDefinitionPersistence>
{
    public void Configure(EntityTypeBuilder<AttributeDefinitionPersistence> builder)
    {
        builder.ToTable("AttributeDefinitions");
        builder.HasKey(attribute => attribute.Id);
        builder.Property(attribute => attribute.Id).ValueGeneratedNever();
        builder.Property(attribute => attribute.ProductTypeId).IsRequired();
        builder.Property(attribute => attribute.Code).IsRequired().HasMaxLength(64);
        builder.Property(attribute => attribute.DisplayName).IsRequired();
        builder.Property(attribute => attribute.DataType).IsRequired();
        builder.Property(attribute => attribute.Scope).IsRequired();
        builder.Property(attribute => attribute.IsRequired).IsRequired();
        builder.Property(attribute => attribute.IsFilterable).IsRequired();

        builder.HasIndex(attribute => new { attribute.ProductTypeId, attribute.Code })
            .IsUnique();
    }
}