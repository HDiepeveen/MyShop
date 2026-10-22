using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface IProductDeleter
{
    Task<bool> DeleteAsync(ProductId productId, CancellationToken cancellationToken);
}
