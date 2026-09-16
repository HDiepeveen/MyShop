using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProduct;

public sealed class GetProduct
{
    private readonly IProductRepository _products;

    public GetProduct(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<GetProductResult> ExecuteAsync(
        GetProductQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(query.ProductId));

        var snapshot = await _products.GetByIdAsync(query.ProductId, cancellationToken);
        return snapshot is null
            ? GetProductResult.Failed(GetProductFailure.ProductNotFound)
            : GetProductResult.Succeeded(snapshot);
    }
}
