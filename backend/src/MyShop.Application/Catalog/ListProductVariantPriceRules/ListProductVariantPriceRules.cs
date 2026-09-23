using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ListProductVariantPriceRules;

public sealed class ListProductVariantPriceRules
{
    private readonly IProductRepository _products;

    public ListProductVariantPriceRules(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<ListProductVariantPriceRulesResult> ExecuteAsync(
        ListProductVariantPriceRulesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(query.ProductId));
        if (query.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(query.ProductVariantId));

        var snapshot = await _products.GetByIdAsync(query.ProductId, cancellationToken);
        if (snapshot is null)
            return ListProductVariantPriceRulesResult.Failed(ListProductVariantPriceRulesFailure.ProductNotFound);

        var product = snapshot.Product;

        var variant = product.Variants.SingleOrDefault(candidate => candidate.Id == query.ProductVariantId);
        if (variant is null)
            return ListProductVariantPriceRulesResult.Failed(ListProductVariantPriceRulesFailure.VariantNotFound);

        var rules = variant.PriceRules.OrderByDescending(rule => rule.Priority).ThenBy(rule => rule.Id).ToArray();
        return ListProductVariantPriceRulesResult.Succeeded(
            new PriceRuleListSnapshot(Array.AsReadOnly(rules), snapshot.ConcurrencyToken.Revision));
    }
}
