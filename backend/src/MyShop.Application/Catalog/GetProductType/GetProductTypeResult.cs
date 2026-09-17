using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductType;

public sealed class GetProductTypeResult
{
    private GetProductTypeResult(ProductType? productType, GetProductTypeFailure? failure)
    {
        ProductType = productType;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public ProductType? ProductType { get; }
    public GetProductTypeFailure? Failure { get; }

    public static GetProductTypeResult Succeeded(ProductType productType)
    {
        ArgumentNullException.ThrowIfNull(productType);
        return new(productType, null);
    }

    public static GetProductTypeResult Failed(GetProductTypeFailure failure) =>
        new(null, failure);
}
