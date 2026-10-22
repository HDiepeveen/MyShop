using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.GetStorefrontProduct;

public sealed record GetStorefrontProductQuery(ProductId ProductId);

public sealed class GetStorefrontProduct(IStorefrontCatalog catalog)
{
    private readonly IStorefrontCatalog catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    public Task<StorefrontProduct?> ExecuteAsync(GetStorefrontProductQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.ProductId == default) throw new ArgumentException("Product ID is required.", nameof(query));
        return catalog.GetAsync(query.ProductId, cancellationToken);
    }
}
