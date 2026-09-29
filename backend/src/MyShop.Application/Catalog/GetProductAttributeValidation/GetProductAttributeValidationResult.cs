using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductAttributeValidation;

public sealed class GetProductAttributeValidationResult
{
    private GetProductAttributeValidationResult(
        IReadOnlyList<ProductAttributeIssue>? issues, GetProductAttributeValidationFailure? failure)
    {
        Issues = issues;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public IReadOnlyList<ProductAttributeIssue>? Issues { get; }
    public GetProductAttributeValidationFailure? Failure { get; }

    public static GetProductAttributeValidationResult Succeeded(IReadOnlyList<ProductAttributeIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        return new(issues.ToList().AsReadOnly(), null);
    }

    public static GetProductAttributeValidationResult Failed(GetProductAttributeValidationFailure failure) =>
        new(null, failure);
}
