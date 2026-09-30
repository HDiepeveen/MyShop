using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class OrderLinePersistenceConfiguration : IEntityTypeConfiguration<OrderLinePersistence>
{
    public void Configure(EntityTypeBuilder<OrderLinePersistence> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(line => new { line.OrderId, line.Ordinal });
        builder.Property(line => line.OrderId).ValueGeneratedNever();
        builder.Property(line => line.Ordinal).ValueGeneratedNever();
        builder.Property(line => line.ProductId).IsRequired().ValueGeneratedNever();
        builder.Property(line => line.VariantId).IsRequired().ValueGeneratedNever();
        builder.Property(line => line.ProductName).IsRequired();
        builder.Property(line => line.VariantName).IsRequired();
        builder.Property(line => line.Quantity).IsRequired();
        builder.Property(line => line.UnitAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(line => line.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(line => line.TotalAmount).IsRequired().HasPrecision(20, 2);
        builder.HasOne(line => line.Order).WithMany(order => order.Lines)
            .HasForeignKey(line => line.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
