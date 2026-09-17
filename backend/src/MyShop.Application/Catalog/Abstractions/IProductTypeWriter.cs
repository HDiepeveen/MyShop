using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductTypeWriter
{
    Task AddAsync(ProductType productType, CancellationToken cancellationToken);
    Task SaveAsync(ProductType productType, CancellationToken cancellationToken);
}
