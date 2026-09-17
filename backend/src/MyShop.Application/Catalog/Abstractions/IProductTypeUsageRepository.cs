using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductTypeUsageRepository
{
    Task<int> CountProductsAsync(ProductTypeId productTypeId, CancellationToken cancellationToken);
}
