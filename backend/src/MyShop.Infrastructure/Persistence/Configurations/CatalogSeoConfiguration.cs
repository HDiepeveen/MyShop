using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;
namespace MyShop.Infrastructure.Persistence.Configurations;
internal sealed class CatalogSeoSettingsPersistenceConfiguration : IEntityTypeConfiguration<CatalogSeoSettingsPersistence>
{
    internal static readonly Guid Id = Guid.Parse("a178498a-a53f-48fb-9650-d0f3d3108322");
    public void Configure(EntityTypeBuilder<CatalogSeoSettingsPersistence> builder)
    {
        builder.ToTable("CatalogSeoSettings"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ShopName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WelcomeText).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Introduction).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.FooterText).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CompanyHeading).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.CompanyDescription).HasMaxLength(4000);
        builder.Property(x => x.CompanyAddress).HasMaxLength(500);
        builder.Property(x => x.CompanyEmail).HasMaxLength(254);
        builder.Property(x => x.CompanyPhone).HasMaxLength(100);
        builder.Property(x => x.CompanyOpeningHours).HasMaxLength(1000);
        builder.Property(x => x.Heading).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SeoTitle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken().ValueGeneratedNever();
        builder.HasData(new CatalogSeoSettingsPersistence { Id = Id, Version = Guid.Parse("5509887c-93b7-4d70-93db-8151db89fa01") });
    }
}
internal sealed class ProductSeoPersistenceConfiguration : IEntityTypeConfiguration<ProductSeoPersistence>
{
    public void Configure(EntityTypeBuilder<ProductSeoPersistence> builder)
    {
        builder.ToTable("ProductSeos"); builder.HasKey(x => x.ProductId);
        builder.Property(x => x.ProductId).ValueGeneratedNever();
        builder.Property(x => x.SeoTitle).HasMaxLength(200);
        builder.Property(x => x.SeoDescription).HasMaxLength(500);
        builder.Property(x => x.WebAddress).HasMaxLength(160);
        builder.Property(x => x.Version).IsConcurrencyToken().ValueGeneratedNever();
        builder.HasOne(x => x.Product).WithOne(x => x.Seo).HasForeignKey<ProductSeoPersistence>(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
internal sealed class ProductWebAddressPersistenceConfiguration : IEntityTypeConfiguration<ProductWebAddressPersistence>
{
    public void Configure(EntityTypeBuilder<ProductWebAddressPersistence> builder)
    {
        builder.ToTable("ProductWebAddresses"); builder.HasKey(x => x.Address);
        builder.Property(x => x.Address).HasMaxLength(160).ValueGeneratedNever();
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ProductId);
    }
}
