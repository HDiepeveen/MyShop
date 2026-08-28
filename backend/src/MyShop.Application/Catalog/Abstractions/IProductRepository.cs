using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken);

    Task SaveAsync(Product product, CancellationToken cancellationToken);
}
