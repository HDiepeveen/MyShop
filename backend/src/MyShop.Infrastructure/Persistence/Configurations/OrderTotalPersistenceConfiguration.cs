using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class OrderTotalPersistenceConfiguration : IEntityTypeConfiguration<OrderTotalPersistence>
{
    public void Configure(EntityTypeBuilder<OrderTotalPersistence> builder)
    {
        builder.ToTable("OrderTotals");
        builder.HasKey(total => new { total.OrderId, total.Currency });
        builder.Property(total => total.OrderId).ValueGeneratedNever();
        builder.Property(total => total.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(total => total.Amount).IsRequired().HasPrecision(22, 2);
        builder.HasOne(total => total.Order).WithMany(order => order.Totals)
            .HasForeignKey(total => total.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
