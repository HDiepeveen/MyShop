using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using HDWebFramework.Application.Catalog.CategoryNavigation;
namespace HDWebFramework.Infrastructure.Persistence.Repositories;
internal sealed class CategoryNavigationStore(HDWebFrameworkDbContext context) : ICategoryNavigationStore
{
    public async Task<CategoryPageInfo?> ResolvePageAsync(string address, CancellationToken ct)
    {
        var key = address.ToLowerInvariant();
        var id = Guid.TryParse(key, out var parsed) ? parsed : Guid.Empty;
        return await context.Categories.AsNoTracking().Where(x => x.WebAddress == key || x.Id == id)
            .Select(x => new CategoryPageInfo(x.Id, x.Name, x.WebAddress ?? x.Id.ToString(), x.SeoTitle, x.SeoDescription)).SingleOrDefaultAsync(ct);
    }
    public Task<ShopNavigationSettings> GetShopAsync(CancellationToken ct) => context.CatalogSeoSettings.AsNoTracking()
        .Where(x => x.Id == HDWebFramework.Infrastructure.Persistence.Configurations.CatalogSeoSettingsPersistenceConfiguration.Id)
        .Select(x => new ShopNavigationSettings(x.ShowAssortmentInMenu, x.NavigationRevision)).SingleAsync(ct);
    public async Task<CategoryNavigationResult> SaveShopAsync(ShopNavigationSettings settings, CancellationToken ct)
    {
        var changed = await context.CatalogSeoSettings.Where(x => x.Id == HDWebFramework.Infrastructure.Persistence.Configurations.CatalogSeoSettingsPersistenceConfiguration.Id && x.NavigationRevision == settings.Revision)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.ShowAssortmentInMenu, settings.ShowAssortmentInMenu)
                .SetProperty(x => x.NavigationRevision, Guid.NewGuid()), ct);
        return new(changed == 1 ? null : CategoryNavigationFailure.Conflict);
    }
    public Task<CategoryNavigationSettings?> GetAsync(Guid id, CancellationToken ct) => context.Categories.AsNoTracking()
        .Where(x => x.Id == id).Select(x => new CategoryNavigationSettings(x.Id, x.ShowInMenu, x.NavigationRevision) { WebAddress = x.WebAddress, SeoTitle = x.SeoTitle, SeoDescription = x.SeoDescription }).SingleOrDefaultAsync(ct);
    public async Task<CategoryNavigationResult> SaveAsync(CategoryNavigationSettings settings, CancellationToken ct)
    {
        settings = settings.Normalize();
        if (settings.WebAddress is not null && await context.Categories.AnyAsync(x => x.Id != settings.CategoryId && x.WebAddress == settings.WebAddress, ct)) return new(CategoryNavigationFailure.AddressInUse);
        try
        {
        var changed = await context.Categories.Where(x => x.Id == settings.CategoryId && x.NavigationRevision == settings.Revision)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.ShowInMenu, settings.ShowInMenu)
                .SetProperty(x => x.WebAddress, settings.WebAddress).SetProperty(x => x.SeoTitle, settings.SeoTitle).SetProperty(x => x.SeoDescription, settings.SeoDescription)
                .SetProperty(x => x.NavigationRevision, Guid.NewGuid()), ct);
        return new(changed == 1 ? null : await context.Categories.AnyAsync(x => x.Id == settings.CategoryId, ct) ? CategoryNavigationFailure.Conflict : CategoryNavigationFailure.NotFound);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627) { return new(CategoryNavigationFailure.AddressInUse); }
    }
    public async Task<IReadOnlyList<CategoryNavigationLink>> ListVisibleAsync(CancellationToken ct) => await context.Categories.AsNoTracking()
        .Where(x => x.ShowInMenu).OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new CategoryNavigationLink(x.Id, x.Name) { WebAddress = x.WebAddress ?? x.Id.ToString() }).ToListAsync(ct);
}
