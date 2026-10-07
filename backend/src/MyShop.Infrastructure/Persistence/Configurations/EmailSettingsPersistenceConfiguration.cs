using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class EmailSettingsPersistenceConfiguration : IEntityTypeConfiguration<EmailSettingsPersistence>
{
    internal static readonly Guid Id = Guid.Parse("13c5dc48-ce9a-44ef-9fb4-5c6d369cfeb6");
    internal static readonly Guid Version = Guid.Parse("88b83c3e-e6b5-43a6-85b2-6f2d7553dd7a");
    public void Configure(EntityTypeBuilder<EmailSettingsPersistence> builder)
    {
        builder.ToTable("EmailSettings"); builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.Host).HasMaxLength(253).IsRequired();
        builder.Property(row => row.UserName).HasMaxLength(320).IsRequired();
        builder.Property(row => row.FromAddress).HasMaxLength(320).IsRequired();
        builder.Property(row => row.FromName).HasMaxLength(200).IsRequired();
        builder.Property(row => row.PublicBaseUrl).HasMaxLength(2000).IsRequired();
        builder.Property(row => row.Version).ValueGeneratedNever().IsConcurrencyToken();
        builder.HasData(new EmailSettingsPersistence { Id = Id, Port = 587, Version = Version });
    }
}
