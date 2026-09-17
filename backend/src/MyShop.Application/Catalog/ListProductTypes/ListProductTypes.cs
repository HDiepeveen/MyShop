using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ListProductTypes;

public sealed class ListProductTypes
{
    private readonly IProductTypeListRepository _productTypes;

    public ListProductTypes(IProductTypeListRepository productTypes) =>
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));

    public async Task<IReadOnlyList<ProductTypeListItem>> ExecuteAsync(
        ListProductTypesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.SearchTerm is not null && string.IsNullOrWhiteSpace(query.SearchTerm))
            throw new ArgumentException("Search term must not be empty or whitespace.", nameof(query.SearchTerm));
        return await _productTypes.ListAsync(query.SearchTerm?.Trim(), cancellationToken);
    }
}
