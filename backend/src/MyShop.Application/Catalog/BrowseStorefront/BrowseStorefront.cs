using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.BrowseStorefront;

public sealed record BrowseStorefrontQuery(int Offset = 0, int Limit = 20, string? Search = null);

public sealed class BrowseStorefront(IStorefrontCatalog catalog)
{
    private readonly IStorefrontCatalog catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    public Task<StorefrontPage> ExecuteAsync(BrowseStorefrontQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Offset < 0 || query.Limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(query));
        var search = query.Search?.Trim();
        if (search?.Length > 200) throw new ArgumentException("Search must not exceed 200 characters.", nameof(query));
        return catalog.ListAsync(query.Offset, query.Limit, string.IsNullOrEmpty(search) ? null : search, cancellationToken);
    }
}
