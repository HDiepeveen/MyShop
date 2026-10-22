using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public sealed class ProductConcurrencyException : Exception
{
    public ProductConcurrencyException(ProductId productId)
        : base($"Product '{productId}' has changed or was deleted since it was read.")
    {
        if (productId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(productId));

        ProductId = productId;
    }

    public ProductId ProductId { get; }
}
