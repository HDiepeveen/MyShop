namespace HDWebFramework.Application.Catalog.CategoryNavigation;

public sealed record CategoryNavigationSettings(Guid CategoryId, bool ShowInMenu, Guid Revision)
{
    public string? WebAddress { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public CategoryNavigationSettings Normalize()
    {
        var address = HDWebFramework.Application.Catalog.Seo.SeoText.Address(WebAddress);
        if (address is not null && Guid.TryParse(address, out _)) throw new ArgumentException("Gebruik woorden in het webadres, geen categorie-id.");
        return this with { WebAddress = address, SeoTitle = HDWebFramework.Application.Catalog.Seo.SeoText.Optional(SeoTitle, 200), SeoDescription = HDWebFramework.Application.Catalog.Seo.SeoText.Optional(SeoDescription, 500) };
    }
}
public sealed record CategoryPageInfo(Guid Id, string Name, string WebAddress, string? SeoTitle, string? SeoDescription);
public sealed record CategoryNavigationLink(Guid Id, string Name) { public string? WebAddress { get; init; } }
public enum CategoryNavigationFailure { NotFound, Conflict, AddressInUse }
public interface ICategoryNavigationStore
{
    Task<CategoryPageInfo?> ResolvePageAsync(string address, CancellationToken ct);
    Task<ShopNavigationSettings> GetShopAsync(CancellationToken ct);
    Task<CategoryNavigationResult> SaveShopAsync(ShopNavigationSettings settings, CancellationToken ct);
    Task<CategoryNavigationSettings?> GetAsync(Guid id, CancellationToken ct);
    Task<CategoryNavigationResult> SaveAsync(CategoryNavigationSettings settings, CancellationToken ct);
    Task<IReadOnlyList<CategoryNavigationLink>> ListVisibleAsync(CancellationToken ct);
}

public sealed record CategoryNavigationResult(CategoryNavigationFailure? Failure);

public sealed record ShopNavigationSettings(bool ShowAssortmentInMenu, Guid Revision);
