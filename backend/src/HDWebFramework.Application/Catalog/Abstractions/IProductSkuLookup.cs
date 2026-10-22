using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface IProductSkuLookup
{
    Task<ProductSkuOwner?> FindOwnerAsync(
        Sku sku,
        CancellationToken cancellationToken);
}