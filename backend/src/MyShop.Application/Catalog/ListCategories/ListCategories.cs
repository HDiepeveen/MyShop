using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ListCategories;

public sealed class ListCategories
{
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 100;

    private readonly ICategoryListRepository _categories;

    public ListCategories(ICategoryListRepository categories) =>
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));

    public async Task<IReadOnlyList<CategoryListItem>> ExecuteAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query.Offset), "Offset must not be negative.");
        if (query.Limit is < 1 or > MaximumLimit)
            throw new ArgumentOutOfRangeException(nameof(query.Limit), $"Limit must be between 1 and {MaximumLimit}.");
        if (query.SearchTerm is not null && string.IsNullOrWhiteSpace(query.SearchTerm))
            throw new ArgumentException("Search term must not be empty or whitespace.", nameof(query.SearchTerm));
        if (query.ParentCategoryId == default(CategoryId))
            throw new ArgumentException("Parent category ID must not be empty.", nameof(query.ParentCategoryId));
        if (query.RootsOnly && query.ParentCategoryId is not null)
            throw new ArgumentException(
                "Root and parent category filters cannot be combined.", nameof(query.RootsOnly));

        return await _categories.ListAsync(
            query.Offset,
            query.Limit,
            query.SearchTerm?.Trim(),
            query.ParentCategoryId,
            query.RootsOnly,
            cancellationToken);
    }
}
