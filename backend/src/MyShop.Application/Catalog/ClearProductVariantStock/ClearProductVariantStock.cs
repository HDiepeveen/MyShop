using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ClearProductVariantStock;

public sealed record ClearProductVariantStockCommand(ProductId ProductId,
    ProductVariantId ProductVariantId);
public enum ClearProductVariantStockFailure { ProductNotFound, VariantNotFound }
public sealed record ClearProductVariantStockResult(ClearProductVariantStockFailure? Failure);

public sealed class ClearProductVariantStock(IProductRepository products)
{
    private readonly IProductRepository products = products
        ?? throw new ArgumentNullException(nameof(products));

    public async Task<ClearProductVariantStockResult> ExecuteAsync(ClearProductVariantStockCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductId == default || command.ProductVariantId == default)
            throw new ArgumentException("Product and variant IDs are required.", nameof(command));
        var snapshot = await products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null) return new(ClearProductVariantStockFailure.ProductNotFound);
        var variant = snapshot.Product.Variants.SingleOrDefault(item =>
            item.Id == command.ProductVariantId);
        if (variant is null) return new(ClearProductVariantStockFailure.VariantNotFound);
        if (!variant.TracksStock) return new(null);
        snapshot.Product.ClearVariantStockTracking(command.ProductVariantId);
        await products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);
        return new(null);
    }
}
