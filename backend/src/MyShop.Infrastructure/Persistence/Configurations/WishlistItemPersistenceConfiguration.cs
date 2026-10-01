using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class WishlistItemPersistenceConfiguration : IEntityTypeConfiguration<WishlistItemPersistence>
{
    public void Configure(EntityTypeBuilder<WishlistItemPersistence> builder)
    {
        builder.ToTable("WishlistItems");
        builder.HasKey(item => new { item.UserId, item.ProductId });
        builder.Property(item => item.UserId).HasMaxLength(450);
        builder.Property(item => item.AddedAt).IsRequired();
        builder.HasOne<IdentityUser>().WithMany().HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.UserId, item.AddedAt });
    }
}
