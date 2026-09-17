using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface ICategoryListRepository
{
    Task<IReadOnlyList<CategoryListItem>> ListAsync(
        string? searchTerm,
        CategoryId? parentCategoryId,
        bool rootsOnly,
        CancellationToken cancellationToken);
}

public sealed record CategoryListItem(Guid Id, string Name, Guid? ParentCategoryId);
