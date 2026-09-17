using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ClearProductVariantSku;

public sealed class ClearProductVariantSku
{
    private readonly IProductRepository _products;

    public ClearProductVariantSku(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<ClearProductVariantSkuResult> ExecuteAsync(
        ClearProductVariantSkuCommand command,
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
            return ClearProductVariantSkuResult.Failed(ClearProductVariantSkuFailure.ProductNotFound);

        var variant = snapshot.Product.Variants.SingleOrDefault(candidate =>
            candidate.Id == command.ProductVariantId);
        if (variant is null)
            return ClearProductVariantSkuResult.Failed(ClearProductVariantSkuFailure.VariantNotFound);

        if (variant.Sku is null)
            return ClearProductVariantSkuResult.Succeeded;

        snapshot.Product.ClearVariantSku(command.ProductVariantId);
        await _products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);

        return ClearProductVariantSkuResult.Succeeded;
    }
}
