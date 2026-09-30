using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.BrowseStorefront;

public sealed record BrowseStorefrontQuery(int Offset = 0, int Limit = 20, string? Search = null,
    CategoryId? CategoryId = null, DateTimeOffset? At = null);

public sealed class BrowseStorefront(IStorefrontCatalog catalog)
{
    private readonly IStorefrontCatalog catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    public Task<StorefrontPage> ExecuteAsync(BrowseStorefrontQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Offset < 0 || query.Limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(query));
        if (query.CategoryId is { } categoryId && categoryId == default)
            throw new ArgumentException("Category ID is required.", nameof(query));
        if (query.At is null || query.At == default)
            throw new ArgumentException("Pricing instant is required.", nameof(query));
        var search = query.Search?.Trim();
        if (search?.Length > 200) throw new ArgumentException("Search must not exceed 200 characters.", nameof(query));
        return catalog.ListAsync(query.Offset, query.Limit, string.IsNullOrEmpty(search) ? null : search,
            query.CategoryId, query.At.Value.ToUniversalTime(), cancellationToken);
    }
}
