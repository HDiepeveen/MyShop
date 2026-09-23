using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ClearProductVariantPrice;

public sealed class ClearProductVariantPrice
{
    private readonly IProductRepository _products;

    public ClearProductVariantPrice(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<ClearProductVariantPriceResult> ExecuteAsync(
        ClearProductVariantPriceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));
        if (command.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(command.ProductVariantId));

        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null)
            return ClearProductVariantPriceResult.Failed(ClearProductVariantPriceFailure.ProductNotFound);

        var variant = snapshot.Product.Variants.SingleOrDefault(candidate =>
            candidate.Id == command.ProductVariantId);
        if (variant is null)
            return ClearProductVariantPriceResult.Failed(ClearProductVariantPriceFailure.VariantNotFound);

        if (variant.Price is null)
            return ClearProductVariantPriceResult.Succeeded;

        snapshot.Product.ClearVariantPrice(command.ProductVariantId);
        await _products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);

        return ClearProductVariantPriceResult.Succeeded;
    }
}
