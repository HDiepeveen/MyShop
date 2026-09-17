using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetCategory;

public sealed class GetCategory
{
    private readonly ICategoryRepository _categories;

    public GetCategory(ICategoryRepository categories) =>
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));

    public async Task<GetCategoryResult> ExecuteAsync(
        GetCategoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.CategoryId == default)
            throw new ArgumentException("Category ID must not be empty.", nameof(query.CategoryId));

        var category = await _categories.GetByIdAsync(query.CategoryId, cancellationToken);
        return category is null
            ? GetCategoryResult.Failed(GetCategoryFailure.CategoryNotFound)
            : GetCategoryResult.Succeeded(category);
    }
}
