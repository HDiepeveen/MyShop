using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public sealed class ProductSnapshot
{
    public ProductSnapshot(Product product, ProductConcurrencyToken concurrencyToken)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(concurrencyToken);
        if (product.Id != concurrencyToken.ProductId)
            throw new ArgumentException("Concurrency token must belong to the product.", nameof(concurrencyToken));

        Product = product;
        ConcurrencyToken = concurrencyToken;
    }

    public Product Product { get; }
    public ProductConcurrencyToken ConcurrencyToken { get; }
}
