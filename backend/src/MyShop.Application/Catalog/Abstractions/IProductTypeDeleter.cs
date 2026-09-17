using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductTypeDeleter
{
    Task DeleteAsync(ProductTypeId productTypeId, CancellationToken cancellationToken);
}
