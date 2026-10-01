using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class OnlinePaymentStartTotalPersistenceConfiguration : IEntityTypeConfiguration<OnlinePaymentStartTotalPersistence>
{
    public void Configure(EntityTypeBuilder<OnlinePaymentStartTotalPersistence> builder)
    {
        builder.ToTable("OnlinePaymentStartTotals");
        builder.HasKey(total => new { total.OnlinePaymentStartId, total.Currency });
        builder.Property(total => total.OnlinePaymentStartId).HasColumnName("CheckoutToken");
        builder.Property(total => total.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(total => total.Amount).HasPrecision(18, 2);
        builder.HasOne(total => total.PaymentStart).WithMany(payment => payment.Totals)
            .HasForeignKey(total => total.OnlinePaymentStartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}