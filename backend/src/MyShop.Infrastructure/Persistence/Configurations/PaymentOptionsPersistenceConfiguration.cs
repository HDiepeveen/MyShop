using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class PaymentOptionsPersistenceConfiguration : IEntityTypeConfiguration<PaymentOptionsPersistence>
{
    public static readonly Guid SingletonId = Guid.Parse("5d484f50-9c6e-4e67-b5e0-3615cdb869eb");
    public static readonly Guid InitialVersion = Guid.Parse("7732cb95-5f60-4f20-82f7-499a020a9d3e");

    public void Configure(EntityTypeBuilder<PaymentOptionsPersistence> builder)
    {
        builder.ToTable("PaymentOptions");
        builder.HasKey(options => options.Id);
        builder.Property(options => options.Id).ValueGeneratedNever();
        builder.Property(options => options.PayLaterEnabled).IsRequired();
        builder.Property(options => options.OnlinePaymentEnabled).IsRequired();
        builder.Property(options => options.Version).IsRequired().ValueGeneratedNever().IsConcurrencyToken();
        builder.HasData(new PaymentOptionsPersistence
        {
            Id = SingletonId,
            PayLaterEnabled = true,
            OnlinePaymentEnabled = false,
            Version = InitialVersion
        });
    }
}
