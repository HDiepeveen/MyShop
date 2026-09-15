using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public sealed record ProductConcurrencyToken
{
    private ProductConcurrencyToken(ProductId productId, Guid revision)
    {
        if (productId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(productId));
        if (revision == Guid.Empty)
            throw new ArgumentException("Revision must not be empty.", nameof(revision));

        ProductId = productId;
        Revision = revision;
    }

    public ProductId ProductId { get; }
    public Guid Revision { get; }

    public static ProductConcurrencyToken Create(ProductId productId, Guid revision) =>
        new(productId, revision);
}
