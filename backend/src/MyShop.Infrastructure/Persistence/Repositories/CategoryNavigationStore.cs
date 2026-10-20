using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.CategoryNavigation;
namespace MyShop.Infrastructure.Persistence.Repositories;
internal sealed class CategoryNavigationStore(MyShopDbContext context) : ICategoryNavigationStore
{
    public Task<ShopNavigationSettings> GetShopAsync(CancellationToken ct) => context.CatalogSeoSettings.AsNoTracking()
        .Where(x => x.Id == MyShop.Infrastructure.Persistence.Configurations.CatalogSeoSettingsPersistenceConfiguration.Id)
        .Select(x => new ShopNavigationSettings(x.ShowAssortmentInMenu, x.NavigationRevision)).SingleAsync(ct);
    public async Task<CategoryNavigationResult> SaveShopAsync(ShopNavigationSettings settings, CancellationToken ct)
    {
        var changed = await context.CatalogSeoSettings.Where(x => x.Id == MyShop.Infrastructure.Persistence.Configurations.CatalogSeoSettingsPersistenceConfiguration.Id && x.NavigationRevision == settings.Revision)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.ShowAssortmentInMenu, settings.ShowAssortmentInMenu)
                .SetProperty(x => x.NavigationRevision, Guid.NewGuid()), ct);
        return new(changed == 1 ? null : CategoryNavigationFailure.Conflict);
    }
    public Task<CategoryNavigationSettings?> GetAsync(Guid id, CancellationToken ct) => context.Categories.AsNoTracking()
        .Where(x => x.Id == id).Select(x => new CategoryNavigationSettings(x.Id, x.ShowInMenu, x.NavigationRevision)).SingleOrDefaultAsync(ct);
    public async Task<CategoryNavigationResult> SaveAsync(CategoryNavigationSettings settings, CancellationToken ct)
    {
        var changed = await context.Categories.Where(x => x.Id == settings.CategoryId && x.NavigationRevision == settings.Revision)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.ShowInMenu, settings.ShowInMenu)
                .SetProperty(x => x.NavigationRevision, Guid.NewGuid()), ct);
        return new(changed == 1 ? null : await context.Categories.AnyAsync(x => x.Id == settings.CategoryId, ct) ? CategoryNavigationFailure.Conflict : CategoryNavigationFailure.NotFound);
    }
    public async Task<IReadOnlyList<CategoryNavigationLink>> ListVisibleAsync(CancellationToken ct) => await context.Categories.AsNoTracking()
        .Where(x => x.ShowInMenu).OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new CategoryNavigationLink(x.Id, x.Name)).ToListAsync(ct);
}
