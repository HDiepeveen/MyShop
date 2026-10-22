using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HDWebFramework.Infrastructure.Persistence.Models;

namespace HDWebFramework.Infrastructure.Persistence.Configurations;

internal sealed class ProductTypePersistenceConfiguration : IEntityTypeConfiguration<ProductTypePersistence>
{
    public void Configure(EntityTypeBuilder<ProductTypePersistence> builder)
    {
        builder.ToTable("ProductTypes");
        builder.HasKey(productType => productType.Id);
        builder.Property(productType => productType.Id).ValueGeneratedNever();
        builder.Property(productType => productType.Name).IsRequired();
        builder.Property(x => x.AboutHeading).HasMaxLength(200);
        builder.Property(x => x.AttributesHeading).HasMaxLength(200);
        builder.Property(x => x.SectionHeadingsRevision).ValueGeneratedNever();

        builder.HasMany(productType => productType.AttributeDefinitions)
            .WithOne(attribute => attribute.ProductType)
            .HasForeignKey(attribute => attribute.ProductTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}