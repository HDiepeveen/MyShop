using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProductVariantPrice;

public sealed class GetProductVariantPrice
{
    private readonly IProductRepository _products;

    public GetProductVariantPrice(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<GetProductVariantPriceResult> ExecuteAsync(
        GetProductVariantPriceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(query.ProductId));
        if (query.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(query.ProductVariantId));

        var snapshot = await _products.GetByIdAsync(query.ProductId, cancellationToken);
        if (snapshot is null)
            return GetProductVariantPriceResult.Failed(GetProductVariantPriceFailure.ProductNotFound);
        var variant = snapshot.Product.Variants.SingleOrDefault(candidate => candidate.Id == query.ProductVariantId);
        if (variant is null)
            return GetProductVariantPriceResult.Failed(GetProductVariantPriceFailure.VariantNotFound);
        if (variant.Price is not { } basePrice)
            return GetProductVariantPriceResult.Failed(GetProductVariantPriceFailure.PriceNotSet);

        return GetProductVariantPriceResult.Succeeded(new ProductVariantPriceQuote(
            basePrice, variant.CalculatePrice(query.At), query.At, snapshot.ConcurrencyToken.Revision));
    }
}
