using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ListProductTypes;

public sealed class ListProductTypes
{
    private readonly IProductTypeListRepository _productTypes;

    public ListProductTypes(IProductTypeListRepository productTypes) =>
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));

    public async Task<IReadOnlyList<ProductTypeListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _productTypes.ListAsync(cancellationToken);
    }
}
