using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.AddProductVariant;

public sealed class AddProductVariant
{
    private readonly IProductRepository _products;

    public AddProductVariant(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<AddProductVariantResult> ExecuteAsync(
        AddProductVariantCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));

        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null)
            return AddProductVariantResult.Failed(AddProductVariantFailure.ProductNotFound);

        snapshot.Product.AddVariant(command.Name);
        var token = await _products.SaveAsync(
            snapshot.Product,
            snapshot.ConcurrencyToken,
            cancellationToken);

        return AddProductVariantResult.Succeeded(new ProductSnapshot(snapshot.Product, token));
    }
}
