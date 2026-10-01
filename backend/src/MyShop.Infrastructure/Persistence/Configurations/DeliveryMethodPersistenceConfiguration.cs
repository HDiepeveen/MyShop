using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class DeliveryMethodPersistenceConfiguration : IEntityTypeConfiguration<DeliveryMethodPersistence>
{
    public void Configure(EntityTypeBuilder<DeliveryMethodPersistence> builder)
    {
        builder.ToTable("DeliveryMethods");
        builder.HasKey(method => method.Id);
        builder.Property(method => method.Id).ValueGeneratedNever();
        builder.Property(method => method.Name).IsRequired().HasMaxLength(100);
        builder.Property(method => method.Description).HasMaxLength(500);
        builder.Property(method => method.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(method => method.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(method => method.Enabled).IsRequired();
        builder.Property(method => method.Version).IsRequired().IsConcurrencyToken();
        builder.HasIndex(method => method.Name).IsUnique();
    }
}
