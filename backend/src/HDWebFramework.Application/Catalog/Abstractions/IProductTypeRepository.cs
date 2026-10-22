using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface IProductTypeRepository
{
    Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken);
}
