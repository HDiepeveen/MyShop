using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetCategoryUsage;

public sealed class GetCategoryUsageResult
{
    private GetCategoryUsageResult(CategoryUsage? value, GetCategoryUsageFailure? failure)
    {
        Usage = value;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public CategoryUsage? Usage { get; }
    public GetCategoryUsageFailure? Failure { get; }

    public static GetCategoryUsageResult Succeeded(CategoryUsage value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, null);
    }

    public static GetCategoryUsageResult Failed(GetCategoryUsageFailure failure) => new(null, failure);
}
