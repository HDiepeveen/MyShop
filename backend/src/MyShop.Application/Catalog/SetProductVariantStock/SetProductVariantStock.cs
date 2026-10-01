using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetProductVariantStock;

public sealed record SetProductVariantStockCommand(ProductId ProductId,
    ProductVariantId ProductVariantId, int Quantity);
public enum SetProductVariantStockFailure { ProductNotFound, VariantNotFound }
public sealed record SetProductVariantStockResult(SetProductVariantStockFailure? Failure);

public sealed class SetProductVariantStock(IProductRepository products)
{
    private readonly IProductRepository products = products
        ?? throw new ArgumentNullException(nameof(products));

    public async Task<SetProductVariantStockResult> ExecuteAsync(SetProductVariantStockCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command));
        if (command.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(command));
        if (command.Quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(command));
        var snapshot = await products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null) return new(SetProductVariantStockFailure.ProductNotFound);
        var variant = snapshot.Product.Variants.SingleOrDefault(item =>
            item.Id == command.ProductVariantId);
        if (variant is null) return new(SetProductVariantStockFailure.VariantNotFound);
        if (variant.StockQuantity == command.Quantity) return new(null);
        snapshot.Product.SetVariantStockQuantity(command.ProductVariantId, command.Quantity);
        await products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);
        return new(null);
    }
}
