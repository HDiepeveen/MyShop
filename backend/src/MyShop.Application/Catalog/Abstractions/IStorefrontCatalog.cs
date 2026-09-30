using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IStorefrontCatalog
{
    Task<StorefrontPage> ListAsync(int offset, int limit, string? search, CancellationToken cancellationToken);
    Task<StorefrontProduct?> GetAsync(ProductId id, CancellationToken cancellationToken);
}

public sealed record StorefrontItem(Guid Id, string Name, string? ImageUrl, string ImageAlt);
public sealed record StorefrontPage(IReadOnlyList<StorefrontItem> Items, int TotalCount, int Offset, int Limit);
public sealed record StorefrontVariant(Guid Id, string Name);
public sealed record StorefrontProduct(Guid Id, string Name, string Description, string? ImageUrl, string ImageAlt, IReadOnlyList<StorefrontVariant> Variants);
