using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductVariantPrice;

public sealed record ProductVariantPriceQuote(Money BasePrice, Money Price, DateTimeOffset At, Guid Revision);

public sealed class GetProductVariantPriceResult
{
    private GetProductVariantPriceResult(ProductVariantPriceQuote? quote, GetProductVariantPriceFailure? failure)
    {
        Quote = quote;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public ProductVariantPriceQuote? Quote { get; }
    public GetProductVariantPriceFailure? Failure { get; }

    public static GetProductVariantPriceResult Succeeded(ProductVariantPriceQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);
        return new(quote, null);
    }

    public static GetProductVariantPriceResult Failed(GetProductVariantPriceFailure failure) => new(null, failure);
}
