using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface ICategoryWriter
{
    Task AddAsync(Category category, CancellationToken cancellationToken);
}
