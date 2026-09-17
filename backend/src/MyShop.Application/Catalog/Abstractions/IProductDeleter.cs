using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductDeleter
{
    Task<bool> DeleteAsync(ProductId productId, CancellationToken cancellationToken);
}
