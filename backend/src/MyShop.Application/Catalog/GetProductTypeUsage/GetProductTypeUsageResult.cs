namespace MyShop.Application.Catalog.GetProductTypeUsage;

public sealed class GetProductTypeUsageResult
{
    private GetProductTypeUsageResult(int? value, GetProductTypeUsageFailure? failure)
    {
        ProductCount = value;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public int? ProductCount { get; }
    public GetProductTypeUsageFailure? Failure { get; }

    public static GetProductTypeUsageResult Succeeded(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        return new(value, null);
    }

    public static GetProductTypeUsageResult Failed(GetProductTypeUsageFailure failure) => new(null, failure);
}
