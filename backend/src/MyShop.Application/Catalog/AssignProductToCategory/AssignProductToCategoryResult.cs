namespace MyShop.Application.Catalog.AssignProductToCategory;

public sealed class AssignProductToCategoryResult
{
    private AssignProductToCategoryResult(AssignProductToCategoryFailure? failure) =>
        Failure = failure;

    public bool IsSuccess => Failure is null;
    public AssignProductToCategoryFailure? Failure { get; }

    public static AssignProductToCategoryResult Succeeded { get; } = new(null);

    public static AssignProductToCategoryResult Failed(AssignProductToCategoryFailure failure) =>
        new(failure);
}