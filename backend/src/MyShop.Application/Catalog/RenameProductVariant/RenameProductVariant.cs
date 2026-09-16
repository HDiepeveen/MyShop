using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RenameProductVariant;

public sealed class RenameProductVariant
{
    private readonly IProductRepository _products;

    public RenameProductVariant(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<RenameProductVariantResult> ExecuteAsync(
        RenameProductVariantCommand command,
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
            return RenameProductVariantResult.Failed(RenameProductVariantFailure.ProductNotFound);

        var variant = snapshot.Product.Variants.SingleOrDefault(candidate =>
            candidate.Id == command.ProductVariantId);
        if (variant is null)
            return RenameProductVariantResult.Failed(RenameProductVariantFailure.VariantNotFound);
        if (variant.Name == command.Name)
            return RenameProductVariantResult.Succeeded;

        snapshot.Product.RenameVariant(command.ProductVariantId, command.Name);
        await _products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);

        return RenameProductVariantResult.Succeeded;
    }
}
