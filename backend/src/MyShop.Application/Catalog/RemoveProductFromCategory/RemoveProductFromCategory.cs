using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RemoveProductFromCategory;

public sealed class RemoveProductFromCategory
{
    private readonly IProductRepository _products;

    public RemoveProductFromCategory(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<RemoveProductFromCategoryResult> ExecuteAsync(
        RemoveProductFromCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));
        if (command.CategoryId == default)
            throw new ArgumentException("Category ID must not be empty.", nameof(command.CategoryId));

        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return RemoveProductFromCategoryResult.Failed(RemoveProductFromCategoryFailure.ProductNotFound);

        if (!product.CategoryIds.Contains(command.CategoryId))
            return RemoveProductFromCategoryResult.Succeeded;

        product.RemoveFromCategory(command.CategoryId);
        await _products.SaveAsync(product, cancellationToken);

        return RemoveProductFromCategoryResult.Succeeded;
    }
}