using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class CategoryPersistenceConfiguration : IEntityTypeConfiguration<CategoryPersistence>
{
    public void Configure(EntityTypeBuilder<CategoryPersistence> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).IsRequired();
        builder.Property(category => category.ParentCategoryId).IsRequired(false);

        builder.HasOne(category => category.Parent)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}