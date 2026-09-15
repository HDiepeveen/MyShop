using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class CategoryPersistenceMapper
{
    internal static Category ToDomain(CategoryPersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(persistence);

        CategoryId? parentCategoryId = persistence.ParentCategoryId is null
            ? null
            : CategoryId.From(persistence.ParentCategoryId.Value);

        return Category.Rehydrate(
            CategoryId.From(persistence.Id),
            persistence.Name,
            parentCategoryId);
    }
}