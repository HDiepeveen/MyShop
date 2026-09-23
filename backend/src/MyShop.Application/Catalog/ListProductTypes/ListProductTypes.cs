using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ListProductTypes;

public sealed class ListProductTypes
{
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 100;

    private readonly IProductTypeListRepository _productTypes;

    public ListProductTypes(IProductTypeListRepository productTypes) =>
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));

    public async Task<IReadOnlyList<ProductTypeListItem>> ExecuteAsync(
        ListProductTypesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query.Offset), "Offset must not be negative.");
        if (query.Limit is < 1 or > MaximumLimit)
            throw new ArgumentOutOfRangeException(nameof(query.Limit), $"Limit must be between 1 and {MaximumLimit}.");
        if (query.SearchTerm is not null && string.IsNullOrWhiteSpace(query.SearchTerm))
            throw new ArgumentException("Search term must not be empty or whitespace.", nameof(query.SearchTerm));
        return await _productTypes.ListAsync(query.Offset, query.Limit, query.SearchTerm?.Trim(), cancellationToken);
    }
}
