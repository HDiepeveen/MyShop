using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetStorefrontPrices;

public sealed record GetStorefrontPricesQuery(ProductId ProductId, DateTimeOffset At);
public sealed record StorefrontVariantPrice(Guid VariantId, decimal? Amount, string? Currency);
public sealed record StorefrontPrices(DateTimeOffset At, IReadOnlyList<StorefrontVariantPrice> Variants);

public sealed class GetStorefrontPrices(IProductRepository products)
{
    private readonly IProductRepository products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<StorefrontPrices?> ExecuteAsync(GetStorefrontPricesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.ProductId == default) throw new ArgumentException("Product ID is required.", nameof(query));
        var snapshot = await products.GetByIdAsync(query.ProductId, cancellationToken);
        if (snapshot is null || !snapshot.Product.Presentation.IsPublished) return null;
        var prices = snapshot.Product.Variants.Select(variant =>
        {
            Money? price = variant.Price is null ? null : variant.CalculatePrice(query.At);
            return new StorefrontVariantPrice(variant.Id.Value, price?.Amount, price?.Currency);
        }).ToList();
        return new(query.At, prices);
    }
}
