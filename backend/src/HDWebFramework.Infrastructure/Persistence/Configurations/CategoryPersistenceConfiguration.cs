using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HDWebFramework.Infrastructure.Persistence.Models;

namespace HDWebFramework.Infrastructure.Persistence.Configurations;

internal sealed class CategoryPersistenceConfiguration : IEntityTypeConfiguration<CategoryPersistence>
{
    public void Configure(EntityTypeBuilder<CategoryPersistence> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).IsRequired();
        builder.Property(category => category.WebAddress).HasMaxLength(160);
        builder.Property(category => category.SeoTitle).HasMaxLength(200);
        builder.Property(category => category.SeoDescription).HasMaxLength(500);
        builder.HasIndex(category => category.WebAddress).IsUnique().HasFilter("[WebAddress] IS NOT NULL");
        builder.Property(category => category.ParentCategoryId).IsRequired(false);

        builder.HasOne(category => category.Parent)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}