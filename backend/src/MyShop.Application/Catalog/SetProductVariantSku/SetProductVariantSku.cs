using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetProductVariantSku;

public sealed class SetProductVariantSku
{
    private readonly IProductRepository _products;
    private readonly IProductSkuLookup _skuLookup;

    public SetProductVariantSku(
        IProductRepository products,
        IProductSkuLookup skuLookup)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _skuLookup = skuLookup ?? throw new ArgumentNullException(nameof(skuLookup));
    }

    public async Task<SetProductVariantSkuResult> ExecuteAsync(
        SetProductVariantSkuCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));
        if (command.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(command.ProductVariantId));

        var sku = Sku.Create(command.Sku);
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return SetProductVariantSkuResult.Failed(SetProductVariantSkuFailure.ProductNotFound);

        var variant = product.Variants.SingleOrDefault(candidate =>
            candidate.Id == command.ProductVariantId);
        if (variant is null)
            return SetProductVariantSkuResult.Failed(SetProductVariantSkuFailure.VariantNotFound);

        if (variant.Sku == sku)
            return SetProductVariantSkuResult.Succeeded;

        if (product.Variants.Any(candidate =>
                candidate.Id != command.ProductVariantId && candidate.Sku == sku))
            return SetProductVariantSkuResult.Failed(SetProductVariantSkuFailure.SkuAlreadyInUse);

        var owner = await _skuLookup.FindOwnerAsync(sku, cancellationToken);
        if (owner is not null && (owner.ProductId != command.ProductId
            || owner.ProductVariantId != command.ProductVariantId))
            return SetProductVariantSkuResult.Failed(SetProductVariantSkuFailure.SkuAlreadyInUse);

        product.SetVariantSku(command.ProductVariantId, sku);
        await _products.SaveAsync(product, cancellationToken);

        return SetProductVariantSkuResult.Succeeded;
    }
}