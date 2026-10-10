using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Seo;
using MyShop.Infrastructure.Persistence.Configurations;
using MyShop.Infrastructure.Persistence.Models;
namespace MyShop.Infrastructure.Persistence.Repositories;
internal sealed class CatalogSeoStore(MyShopDbContext context) : ICatalogSeoStore
{
    public Task<ProductTypeHeadings?> GetTypeHeadingsAsync(Guid id, CancellationToken ct) => context.ProductTypes.AsNoTracking()
        .Where(x => x.Id == id).Select(x => new ProductTypeHeadings(x.Id, x.AboutHeading, x.AttributesHeading, x.SectionHeadingsRevision)).SingleOrDefaultAsync(ct);
    public async Task<SeoResult> SaveTypeHeadingsAsync(ProductTypeHeadings value, CancellationToken ct)
    {
        var changed = await context.ProductTypes.Where(x => x.Id == value.ProductTypeId && x.SectionHeadingsRevision == value.Revision)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.AboutHeading, value.AboutHeading)
                .SetProperty(x => x.AttributesHeading, value.AttributesHeading).SetProperty(x => x.SectionHeadingsRevision, Guid.NewGuid()), ct);
        return new(changed == 1 ? null : await context.ProductTypes.AnyAsync(x => x.Id == value.ProductTypeId, ct) ? SeoFailure.Conflict : SeoFailure.NotFound);
    }
    public async Task<ShopSeoSettings> GetSettingsAsync(CancellationToken ct)
    {
        var row = await context.CatalogSeoSettings.AsNoTracking().SingleAsync(x => x.Id == CatalogSeoSettingsPersistenceConfiguration.Id, ct);
        return new(row.Heading, row.SeoTitle, row.Version, row.ShopName, row.WelcomeText, row.Introduction, row.FooterText,
            new(row.CompanyHeading, row.CompanyName, row.CompanyDescription, row.CompanyAddress, row.CompanyEmail, row.CompanyPhone, row.CompanyOpeningHours));
    }
    public async Task<SeoResult> SaveSettingsAsync(ShopSeoSettings value, CancellationToken ct)
    {
        var changed = await context.CatalogSeoSettings.Where(x => x.Id == CatalogSeoSettingsPersistenceConfiguration.Id && x.Version == value.Revision)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Heading, value.Heading).SetProperty(x => x.SeoTitle, value.SeoTitle).SetProperty(x => x.ShopName, value.ShopName).SetProperty(x => x.WelcomeText, value.WelcomeText)
                .SetProperty(x => x.Introduction, value.Introduction).SetProperty(x => x.FooterText, value.FooterText)
                .SetProperty(x => x.CompanyHeading, value.Company.Heading).SetProperty(x => x.CompanyName, value.Company.Name)
                .SetProperty(x => x.CompanyDescription, value.Company.Description).SetProperty(x => x.CompanyAddress, value.Company.Address)
                .SetProperty(x => x.CompanyEmail, value.Company.Email).SetProperty(x => x.CompanyPhone, value.Company.Phone)
                .SetProperty(x => x.CompanyOpeningHours, value.Company.OpeningHours).SetProperty(x => x.Version, Guid.NewGuid()), ct);
        return new(changed == 1 ? null : SeoFailure.Conflict);
    }
    public async Task<ProductSeoInfo?> GetProductAsync(Guid id, CancellationToken ct)
    {
        var row = await context.Products.AsNoTracking().Include(x => x.Seo).Include(x => x.ProductType).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (row is null) return null;
        var settings = await GetSettingsAsync(ct);
        return new(row.Id, row.Name, row.Description, row.ImageUrl, row.ImageAlt, row.IsPublished,
            new(row.Seo?.SeoTitle, row.Seo?.SeoDescription, row.Seo?.WebAddress), row.Seo?.Version ?? Guid.Empty) { ShopName = settings.ShopName, ResolvedAboutHeading = row.ProductType.AboutHeading ?? "Over dit product",
                ResolvedAttributesHeading = row.ProductType.AttributesHeading ?? "Productkenmerken" };
    }
    public async Task<Guid?> ResolveProductAsync(string key, CancellationToken ct)
    {
        if (Guid.TryParseExact(key, "D", out var id) && id != Guid.Empty) return id;
        if (key.Length > 160) return null;
        var address = key.ToLowerInvariant();
        var owner = await context.ProductWebAddresses.AsNoTracking().Where(x => x.Address == address).Select(x => (Guid?)x.ProductId).SingleOrDefaultAsync(ct);
        return owner ?? SeoText.AutomaticId(key);
    }
    public async Task<SeoResult> SaveProductAsync(Guid id, ProductSeoValues value, Guid revision, CancellationToken ct)
    {
        if (!await context.Products.AnyAsync(x => x.Id == id, ct)) return new(SeoFailure.NotFound);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var row = await context.ProductSeos.SingleOrDefaultAsync(x => x.ProductId == id, ct);
        if ((row?.Version ?? Guid.Empty) != revision) return new(SeoFailure.Conflict);
        if (value.WebAddress is not null)
        {
            var address = await context.ProductWebAddresses.SingleOrDefaultAsync(x => x.Address == value.WebAddress, ct);
            if (address is not null && address.ProductId != id) return new(SeoFailure.AddressInUse);
            if (address is null) context.ProductWebAddresses.Add(new() { Address = value.WebAddress, ProductId = id });
        }
        if (row is null) { row = new() { ProductId = id }; context.ProductSeos.Add(row); }
        row.SeoTitle = value.SeoTitle; row.SeoDescription = value.SeoDescription; row.WebAddress = value.WebAddress; row.Version = Guid.NewGuid();
        try { await context.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return new(null); }
        catch (DbUpdateConcurrencyException) { return new(SeoFailure.Conflict); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(ct);
            var current = await context.ProductSeos.AsNoTracking().Where(x => x.ProductId == id).Select(x => (Guid?)x.Version).SingleOrDefaultAsync(ct);
            return new(current is not null && current != revision ? SeoFailure.Conflict : SeoFailure.AddressInUse);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 }) { return new(SeoFailure.NotFound); }
    }
}
