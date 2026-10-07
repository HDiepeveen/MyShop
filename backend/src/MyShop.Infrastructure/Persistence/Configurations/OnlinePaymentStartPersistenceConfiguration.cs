using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class OnlinePaymentStartPersistenceConfiguration : IEntityTypeConfiguration<OnlinePaymentStartPersistence>
{
    public void Configure(EntityTypeBuilder<OnlinePaymentStartPersistence> builder)
    {
        builder.ToTable("OnlinePaymentStarts");
        builder.HasKey(payment => payment.CheckoutToken);
        builder.Property(payment => payment.CheckoutToken).ValueGeneratedNever();
        builder.Property(payment => payment.ProviderName).IsRequired().HasMaxLength(100);
        builder.Property(payment => payment.PaymentReference).IsRequired().HasMaxLength(100);
        builder.Property(payment => payment.ProviderPaymentId).IsRequired().HasMaxLength(200);
        builder.Property(payment => payment.CheckoutUrl).IsRequired().HasMaxLength(2048);
        builder.Property(payment => payment.CustomerUserId).HasMaxLength(450);
        builder.Property(payment => payment.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(payment => payment.Email).IsRequired().HasMaxLength(320);
        builder.Property(payment => payment.AddressLine).IsRequired().HasMaxLength(200);
        builder.Property(payment => payment.PostalCode).IsRequired().HasMaxLength(32);
        builder.Property(payment => payment.City).IsRequired().HasMaxLength(100);
        builder.Property(payment => payment.CountryCode).IsRequired().HasMaxLength(2).IsFixedLength();
        builder.Property(payment => payment.CreatedAt).IsRequired();
        builder.Property(payment => payment.DeliveryMethodName).IsRequired().HasMaxLength(100);
        builder.Property(payment => payment.DeliveryDescription).HasMaxLength(500);
        builder.Property(payment => payment.DeliveryAmount).HasPrecision(18, 2);
        builder.Property(payment => payment.DeliveryCurrency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.HasIndex(payment => payment.PaymentReference).IsUnique();
        builder.HasIndex(payment => payment.ProviderPaymentId);
    }
}