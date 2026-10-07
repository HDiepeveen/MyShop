using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;
namespace MyShop.Infrastructure.Persistence.Configurations;
internal sealed class CompanySettingsPersistenceConfiguration : IEntityTypeConfiguration<CompanySettingsPersistence>
{
    internal static readonly Guid Id = Guid.Parse("36d28057-9e9b-4fb7-8b13-c563b4813168");
    public void Configure(EntityTypeBuilder<CompanySettingsPersistence> builder)
    {
        builder.ToTable("CompanySettings"); builder.HasKey(row => row.Id); builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.Name).HasMaxLength(200).IsRequired(); builder.Property(row => row.AddressLine).HasMaxLength(200).IsRequired();
        builder.Property(row => row.PostalCode).HasMaxLength(32).IsRequired(); builder.Property(row => row.City).HasMaxLength(100).IsRequired();
        builder.Property(row => row.VatId).HasMaxLength(32).IsRequired(); builder.Property(row => row.KvkNumber).HasMaxLength(8).IsRequired();
        builder.Property(row => row.InvoicePrefix).HasMaxLength(20).IsRequired(); builder.Property(row => row.Version).ValueGeneratedNever().IsConcurrencyToken();
        builder.HasData(new CompanySettingsPersistence { Id = Id, Version = Guid.Parse("184b4fba-f5ec-468b-b289-a6a43ef8c85c") });
    }
}
internal sealed class VatRatePersistenceConfiguration : IEntityTypeConfiguration<VatRatePersistence>
{
    public void Configure(EntityTypeBuilder<VatRatePersistence> builder)
    {
        builder.ToTable("VatRates"); builder.HasKey(row => row.Id);
        builder.Property(row => row.Name).HasMaxLength(100).IsRequired(); builder.Property(row => row.Percentage).HasPrecision(5, 2);
        builder.Property(row => row.Version).ValueGeneratedNever().IsConcurrencyToken(); builder.HasIndex(row => new { row.Percentage, row.Exempt }).IsUnique();
        builder.HasData(
            new VatRatePersistence { Id = Guid.Parse("3db8eaab-ed19-4a15-90f1-9824cfc94821"), Name = "21%", Percentage = 21, Enabled = true, Version = Guid.Parse("751ae315-2fa8-4b94-a4f4-60ef0525c821") },
            new VatRatePersistence { Id = Guid.Parse("3db8eaab-ed19-4a15-90f1-9824cfc94809"), Name = "9%", Percentage = 9, Enabled = true, Version = Guid.Parse("751ae315-2fa8-4b94-a4f4-60ef0525c809") },
            new VatRatePersistence { Id = Guid.Parse("3db8eaab-ed19-4a15-90f1-9824cfc94800"), Name = "0%", Percentage = 0, Enabled = true, Version = Guid.Parse("751ae315-2fa8-4b94-a4f4-60ef0525c800") },
            new VatRatePersistence { Id = Guid.Parse("3db8eaab-ed19-4a15-90f1-9824cfc94801"), Name = "Vrijgesteld", Percentage = 0, Exempt = true, Enabled = true, Version = Guid.Parse("751ae315-2fa8-4b94-a4f4-60ef0525c801") });
    }
}
internal sealed class InvoicePersistenceConfiguration : IEntityTypeConfiguration<InvoicePersistence>
{
    public void Configure(EntityTypeBuilder<InvoicePersistence> builder)
    {
        builder.ToTable("Invoices"); builder.HasKey(row => row.Id); builder.Property(row => row.Number).HasMaxLength(64).IsRequired();
        builder.Property(row => row.Document).IsRequired(); builder.HasIndex(row => row.Number).IsUnique(); builder.HasIndex(row => row.OrderId).IsUnique();
    }
}
internal sealed class InvoiceCounterPersistenceConfiguration : IEntityTypeConfiguration<InvoiceCounterPersistence>
{
    internal static readonly Guid Id = Guid.Parse("8f5f305d-2fd4-4b87-bb49-477655e24a91");
    public void Configure(EntityTypeBuilder<InvoiceCounterPersistence> builder)
    {
        builder.ToTable("InvoiceCounters"); builder.HasKey(row => row.Id); builder.Property(row => row.Id).ValueGeneratedNever();
        builder.HasData(new InvoiceCounterPersistence { Id = Id, NextNumber = 1 });
    }
}
