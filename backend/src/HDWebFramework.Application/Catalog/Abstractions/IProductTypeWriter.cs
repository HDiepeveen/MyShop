using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface IProductTypeWriter
{
    Task AddAsync(ProductType productType, CancellationToken cancellationToken);
    Task SaveAsync(ProductType productType, CancellationToken cancellationToken);
}
