using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductRepository
{
    Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken);

    /// <summary>Inserts a new product only and returns its initial token after persistence.</summary>
    Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing product against its original token and returns a replacement token.
    /// The token must belong to the product. Every successful save advances the revision.
    /// </summary>
    /// <exception cref="ProductConcurrencyException">The product changed or was deleted since it was read.</exception>
    Task<ProductConcurrencyToken> SaveAsync(
        Product product,
        ProductConcurrencyToken expectedToken,
        CancellationToken cancellationToken);
}
