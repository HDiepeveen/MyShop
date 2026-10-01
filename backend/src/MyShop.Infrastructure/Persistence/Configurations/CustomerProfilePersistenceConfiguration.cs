using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class CustomerProfilePersistenceConfiguration : IEntityTypeConfiguration<CustomerProfilePersistence>
{
    public void Configure(EntityTypeBuilder<CustomerProfilePersistence> builder)
    {
        builder.ToTable("CustomerProfiles");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.UserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(profile => profile.UserId).IsUnique();
        builder.Property(profile => profile.Name).IsRequired().HasMaxLength(200);
        builder.Property(profile => profile.AddressLine).IsRequired().HasMaxLength(200);
        builder.Property(profile => profile.PostalCode).IsRequired().HasMaxLength(32);
        builder.Property(profile => profile.City).IsRequired().HasMaxLength(100);
        builder.Property(profile => profile.CountryCode).IsRequired().HasMaxLength(2).IsFixedLength();
        builder.Property(profile => profile.Version).IsRequired().IsConcurrencyToken();
        builder.HasOne<IdentityUser>().WithOne().HasForeignKey<CustomerProfilePersistence>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
