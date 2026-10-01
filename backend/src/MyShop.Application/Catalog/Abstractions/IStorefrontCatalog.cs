using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IStorefrontCatalog
{
    Task<StorefrontPage> ListAsync(int offset, int limit, string? search, CategoryId? categoryId,
        DateTimeOffset at, CancellationToken cancellationToken);
    Task<IReadOnlyList<StorefrontCategory>> ListCategoriesAsync(CancellationToken cancellationToken);
    Task<StorefrontProduct?> GetAsync(ProductId id, CancellationToken cancellationToken);
}

public sealed record StorefrontPriceRange(string Currency, decimal MinimumAmount, decimal MaximumAmount);
public sealed record StorefrontItem(Guid Id, string Name, string? ImageUrl, string ImageAlt,
    bool IsAvailable, IReadOnlyList<StorefrontPriceRange> Prices);
public sealed record StorefrontPage(DateTimeOffset At, IReadOnlyList<StorefrontItem> Items,
    int TotalCount, int Offset, int Limit);
public sealed record StorefrontCategory(Guid Id, string Name);
public sealed record StorefrontVariant(Guid Id, string Name, bool IsAvailable);
public sealed record StorefrontProduct(Guid Id, string Name, string Description, string? ImageUrl,
    string ImageAlt, IReadOnlyList<StorefrontCategory> Categories,
    IReadOnlyList<StorefrontVariant> Variants);
