using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface IProductTypeDeleter
{
    Task DeleteAsync(ProductTypeId productTypeId, CancellationToken cancellationToken);
}
