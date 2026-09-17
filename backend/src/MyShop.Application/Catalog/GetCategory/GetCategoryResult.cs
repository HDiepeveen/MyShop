using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetCategory;

public sealed class GetCategoryResult
{
    private GetCategoryResult(Category? category, GetCategoryFailure? failure)
    {
        Category = category;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public Category? Category { get; }
    public GetCategoryFailure? Failure { get; }

    public static GetCategoryResult Succeeded(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return new(category, null);
    }

    public static GetCategoryResult Failed(GetCategoryFailure failure) =>
        new(null, failure);
}
