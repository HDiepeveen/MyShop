using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ListCategories;

public sealed class ListCategories
{
    private readonly ICategoryListRepository _categories;

    public ListCategories(ICategoryListRepository categories) =>
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));

    public async Task<IReadOnlyList<CategoryListItem>> ExecuteAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.SearchTerm is not null && string.IsNullOrWhiteSpace(query.SearchTerm))
            throw new ArgumentException("Search term must not be empty or whitespace.", nameof(query.SearchTerm));
        return await _categories.ListAsync(query.SearchTerm?.Trim(), cancellationToken);
    }
}
