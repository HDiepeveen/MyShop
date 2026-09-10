using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken);
}