using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ListProducts;

public sealed class ListProducts
{
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 100;

    private readonly IProductListRepository _products;

    public ListProducts(IProductListRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<ProductListPage> ExecuteAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query.Offset), "Offset must not be negative.");
        if (query.Limit is < 1 or > MaximumLimit)
            throw new ArgumentOutOfRangeException(
                nameof(query.Limit),
                $"Limit must be between 1 and {MaximumLimit}.");
        if (query.ProductTypeId == default(ProductTypeId))
            throw new ArgumentException("Product type ID must not be empty.", nameof(query.ProductTypeId));
        if (query.CategoryId == default(CategoryId))
            throw new ArgumentException("Category ID must not be empty.", nameof(query.CategoryId));

        return await _products.ListAsync(
            query.Offset,
            query.Limit,
            query.ProductTypeId,
            query.CategoryId,
            cancellationToken);
    }
}
