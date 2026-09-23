using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface ICategoryListRepository
{
    Task<IReadOnlyList<CategoryListItem>> ListAsync(
        int offset,
        int limit,
        string? searchTerm,
        CategoryId? parentCategoryId,
        bool rootsOnly,
        CancellationToken cancellationToken);
}

public sealed record CategoryListItem(
    Guid Id,
    string Name,
    Guid? ParentCategoryId,
    int DirectChildCount);
