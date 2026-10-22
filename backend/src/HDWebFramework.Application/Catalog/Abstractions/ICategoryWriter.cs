using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface ICategoryWriter
{
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task SaveAsync(Category category, CancellationToken cancellationToken);
    Task DeleteAsync(CategoryId categoryId, CancellationToken cancellationToken);
}
