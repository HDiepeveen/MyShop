using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface IProductTypeUsageRepository
{
    Task<int> CountProductsAsync(ProductTypeId productTypeId, CancellationToken cancellationToken);
}
