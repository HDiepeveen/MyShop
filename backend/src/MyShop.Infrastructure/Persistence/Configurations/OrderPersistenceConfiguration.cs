using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class OrderPersistenceConfiguration : IEntityTypeConfiguration<OrderPersistence>
{
    public void Configure(EntityTypeBuilder<OrderPersistence> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.CustomerUserId).HasMaxLength(450);
        builder.HasOne<IdentityUser>().WithMany().HasForeignKey(order => order.CustomerUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(order => order.CheckoutToken).IsRequired().ValueGeneratedNever();
        builder.Property(order => order.Number).IsRequired().HasMaxLength(40);
        builder.Property(order => order.PlacedAt).IsRequired();
        builder.Property(order => order.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(order => order.Email).IsRequired().HasMaxLength(320);
        builder.Property(order => order.AddressLine).IsRequired().HasMaxLength(200);
        builder.Property(order => order.PostalCode).IsRequired().HasMaxLength(32);
        builder.Property(order => order.City).IsRequired().HasMaxLength(100);
        builder.Property(order => order.CountryCode).IsRequired().HasMaxLength(2).IsFixedLength();
        builder.Property(order => order.DeliveryMethodName).HasMaxLength(100);
        builder.Property(order => order.DeliveryDescription).HasMaxLength(500);
        builder.Property(order => order.DeliveryAmount).HasPrecision(18, 2);
        builder.Property(order => order.DeliveryCurrency).HasMaxLength(3).IsFixedLength();
        builder.Property(order => order.PaymentMethod).IsRequired().HasColumnType("int");
        builder.Property(order => order.PaymentInstructions).HasMaxLength(2000);
        builder.Property(order => order.Status).IsRequired().HasColumnType("int");
        builder.Property(order => order.PaidAt);
        builder.Property(order => order.PaymentReference).HasMaxLength(100);
        builder.Property(order => order.ShippedAt);
        builder.Property(order => order.ShippingCarrier).HasMaxLength(100);
        builder.Property(order => order.TrackingCode).HasMaxLength(100);
        builder.Property(order => order.CancelledAt);
        builder.Property(order => order.CancellationReason).HasMaxLength(500);
        builder.Property(order => order.RefundedAt);
        builder.Property(order => order.RefundReference).HasMaxLength(100);
        builder.Property(order => order.RefundReason).HasMaxLength(500);
        builder.Property(order => order.Version).IsRequired().IsConcurrencyToken();
        builder.HasIndex(order => order.CheckoutToken).IsUnique();
        builder.HasIndex(order => order.Number).IsUnique();
    }
}
