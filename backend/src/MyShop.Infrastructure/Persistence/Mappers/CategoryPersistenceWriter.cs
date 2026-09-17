using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class CategoryPersistenceWriter
{
    internal static void Write(Category category, CategoryPersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(persistence);
        if (persistence.Id == Guid.Empty)
            throw new InvalidOperationException("Persisted category ID must not be empty.");
        if (category.Id.Value != persistence.Id)
            throw new ArgumentException("Domain and persistence category IDs must match.", nameof(persistence));
        if (persistence.ParentCategoryId == persistence.Id)
            throw new InvalidOperationException("A persisted category cannot be its own parent.");

        persistence.Name = category.Name;
        persistence.ParentCategoryId = category.ParentCategoryId?.Value;
    }
}
