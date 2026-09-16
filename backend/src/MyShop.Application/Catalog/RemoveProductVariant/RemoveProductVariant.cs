using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RemoveProductVariant;

public sealed class RemoveProductVariant
{
    private readonly IProductRepository _products;

    public RemoveProductVariant(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<RemoveProductVariantResult> ExecuteAsync(
        RemoveProductVariantCommand command,
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
            return RemoveProductVariantResult.Failed(RemoveProductVariantFailure.ProductNotFound);

        var product = snapshot.Product;
        if (!product.Variants.Any(variant => variant.Id == command.ProductVariantId))
            return RemoveProductVariantResult.Failed(RemoveProductVariantFailure.VariantNotFound);
        if (product.Variants.Count == 1)
            return RemoveProductVariantResult.Failed(RemoveProductVariantFailure.LastVariantCannotBeRemoved);

        product.RemoveVariant(command.ProductVariantId);
        await _products.SaveAsync(product, snapshot.ConcurrencyToken, cancellationToken);

        return RemoveProductVariantResult.Succeeded;
    }
}
