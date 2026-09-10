using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductSkuLookup
{
    Task<ProductSkuOwner?> FindOwnerAsync(
        Sku sku,
        CancellationToken cancellationToken);
}