namespace MyShop.Application.Catalog.RemoveProductFromCategory;

public sealed class RemoveProductFromCategoryResult
{
    private RemoveProductFromCategoryResult(RemoveProductFromCategoryFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public RemoveProductFromCategoryFailure? Failure { get; }

    public static RemoveProductFromCategoryResult Succeeded { get; } = new(null);

    public static RemoveProductFromCategoryResult Failed(RemoveProductFromCategoryFailure failure) =>
        new(failure);
}