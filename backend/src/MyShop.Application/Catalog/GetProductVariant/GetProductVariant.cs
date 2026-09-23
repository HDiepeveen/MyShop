using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProductVariant;

public sealed class GetProductVariant
{
    private readonly IProductRepository _products;

    public GetProductVariant(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<GetProductVariantResult> ExecuteAsync(
        GetProductVariantQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(query.ProductId));
        if (query.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(query.ProductVariantId));

        var snapshot = await _products.GetByIdAsync(query.ProductId, cancellationToken);
        if (snapshot is null)
            return GetProductVariantResult.Failed(GetProductVariantFailure.ProductNotFound);

        var product = snapshot.Product;

        var variant = product.Variants.SingleOrDefault(candidate => candidate.Id == query.ProductVariantId);
        if (variant is null)
            return GetProductVariantResult.Failed(GetProductVariantFailure.VariantNotFound);

        return GetProductVariantResult.Succeeded(
            new ProductVariantSnapshot(variant, snapshot.ConcurrencyToken.Revision));
    }
}
