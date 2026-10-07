using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class OnlinePaymentStartLinePersistenceConfiguration : IEntityTypeConfiguration<OnlinePaymentStartLinePersistence>
{
    public void Configure(EntityTypeBuilder<OnlinePaymentStartLinePersistence> builder)
    {
        builder.ToTable("OnlinePaymentStartLines");
        builder.HasKey(line => new { line.OnlinePaymentStartId, line.Ordinal });
        builder.Property(line => line.OnlinePaymentStartId).HasColumnName("CheckoutToken");
        builder.Property(line => line.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(line => line.VariantName).IsRequired().HasMaxLength(200);
        builder.Property(line => line.UnitAmount).HasPrecision(18, 2);
        builder.Property(line => line.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(line => line.TotalAmount).HasPrecision(18, 2);
        builder.Property(line => line.VatRate).HasPrecision(5, 2);
        builder.HasOne(line => line.PaymentStart).WithMany(payment => payment.Lines)
            .HasForeignKey(line => line.OnlinePaymentStartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
