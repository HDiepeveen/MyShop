using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RenameProduct;

public sealed class RenameProduct
{
    private readonly IProductRepository _products;

    public RenameProduct(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<RenameProductResult> ExecuteAsync(
        RenameProductCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));

        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null)
            return RenameProductResult.Failed(RenameProductFailure.ProductNotFound);

        if (snapshot.Product.Name == command.Name)
            return RenameProductResult.Succeeded;

        snapshot.Product.Rename(command.Name);
        await _products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);

        return RenameProductResult.Succeeded;
    }
}
