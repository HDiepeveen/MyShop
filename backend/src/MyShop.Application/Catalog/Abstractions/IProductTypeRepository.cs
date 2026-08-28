using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductTypeRepository
{
    Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken);
}
