using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class PriceRulePersistenceConfiguration : IEntityTypeConfiguration<PriceRulePersistence>
{
    public void Configure(EntityTypeBuilder<PriceRulePersistence> builder)
    {
        builder.ToTable("PriceRules");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).ValueGeneratedNever();
        builder.Property(rule => rule.ProductVariantId).IsRequired().ValueGeneratedNever();
        builder.Property(rule => rule.Name).IsRequired().HasMaxLength(200);
        builder.Property(rule => rule.AdjustmentType).IsRequired().HasColumnType("int");
        builder.Property(rule => rule.Value).IsRequired().HasPrecision(18, 2);
        builder.Property(rule => rule.Priority).IsRequired().HasColumnType("int");
        builder.Property(rule => rule.StartsAt).IsRequired(false);
        builder.Property(rule => rule.EndsAt).IsRequired(false);
        builder.HasOne(rule => rule.ProductVariant)
            .WithMany(variant => variant.PriceRules)
            .HasForeignKey(rule => rule.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rule => new { rule.ProductVariantId, rule.Priority });
    }
}
