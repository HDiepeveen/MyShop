namespace MyShop.Application.Catalog.CategoryNavigation;

public sealed record CategoryNavigationSettings(Guid CategoryId, bool ShowInMenu, Guid Revision);
public sealed record CategoryNavigationLink(Guid Id, string Name);
public enum CategoryNavigationFailure { NotFound, Conflict }
public interface ICategoryNavigationStore
{
    Task<ShopNavigationSettings> GetShopAsync(CancellationToken ct);
    Task<CategoryNavigationResult> SaveShopAsync(ShopNavigationSettings settings, CancellationToken ct);
    Task<CategoryNavigationSettings?> GetAsync(Guid id, CancellationToken ct);
    Task<CategoryNavigationResult> SaveAsync(CategoryNavigationSettings settings, CancellationToken ct);
    Task<IReadOnlyList<CategoryNavigationLink>> ListVisibleAsync(CancellationToken ct);
}

public sealed record CategoryNavigationResult(CategoryNavigationFailure? Failure);

public sealed record ShopNavigationSettings(bool ShowAssortmentInMenu, Guid Revision);
