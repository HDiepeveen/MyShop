using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.DeleteProduct;

public sealed class DeleteProduct
{
    private readonly IProductDeleter _deleter;

    public DeleteProduct(IProductDeleter deleter) =>
        _deleter = deleter ?? throw new ArgumentNullException(nameof(deleter));

    public Task<bool> ExecuteAsync(ProductId productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (productId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(productId));
        return _deleter.DeleteAsync(productId, cancellationToken);
    }
}
