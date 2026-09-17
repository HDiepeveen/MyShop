using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProductBySku;

public sealed class GetProductBySku
{
    private readonly IProductSkuLookup _skuLookup;

    public GetProductBySku(IProductSkuLookup skuLookup) =>
        _skuLookup = skuLookup ?? throw new ArgumentNullException(nameof(skuLookup));

    public async Task<ProductSkuOwner?> ExecuteAsync(
        GetProductBySkuQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Sku);
        cancellationToken.ThrowIfCancellationRequested();
        return await _skuLookup.FindOwnerAsync(query.Sku, cancellationToken);
    }
}
