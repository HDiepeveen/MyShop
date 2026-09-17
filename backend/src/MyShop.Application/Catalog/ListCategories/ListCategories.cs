using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ListCategories;

public sealed class ListCategories
{
    private readonly ICategoryListRepository _categories;

    public ListCategories(ICategoryListRepository categories) =>
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));

    public async Task<IReadOnlyList<CategoryListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _categories.ListAsync(cancellationToken);
    }
}
