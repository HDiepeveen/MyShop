using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.AssignProductToCategory;

public sealed class AssignProductToCategory
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;

    public AssignProductToCategory(
        IProductRepository products,
        ICategoryRepository categories)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
    }

    public async Task<AssignProductToCategoryResult> ExecuteAsync(
        AssignProductToCategoryCommand command,
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
            return AssignProductToCategoryResult.Failed(AssignProductToCategoryFailure.ProductNotFound);

        var category = await _categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return AssignProductToCategoryResult.Failed(AssignProductToCategoryFailure.CategoryNotFound);

        if (product.CategoryIds.Contains(command.CategoryId))
            return AssignProductToCategoryResult.Succeeded;

        product.AssignToCategory(command.CategoryId);
        await _products.SaveAsync(product, cancellationToken);

        return AssignProductToCategoryResult.Succeeded;
    }
}