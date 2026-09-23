using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProductVariantPriceRule;

public sealed class GetProductVariantPriceRule
{
    private readonly IProductRepository _products;

    public GetProductVariantPriceRule(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<GetProductVariantPriceRuleResult> ExecuteAsync(
        GetProductVariantPriceRuleQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(query.ProductId));
        if (query.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(query.ProductVariantId));
        if (query.PriceRuleId == default)
            throw new ArgumentException("Price rule ID must not be empty.", nameof(query.PriceRuleId));

        var snapshot = await _products.GetByIdAsync(query.ProductId, cancellationToken);
        if (snapshot is null)
            return GetProductVariantPriceRuleResult.Failed(GetProductVariantPriceRuleFailure.ProductNotFound);

        var product = snapshot.Product;

        var variant = product.Variants.SingleOrDefault(candidate => candidate.Id == query.ProductVariantId);
        if (variant is null)
            return GetProductVariantPriceRuleResult.Failed(GetProductVariantPriceRuleFailure.VariantNotFound);

        var rule = variant.PriceRules.SingleOrDefault(candidate => candidate.Id == query.PriceRuleId);
        return rule is null
            ? GetProductVariantPriceRuleResult.Failed(GetProductVariantPriceRuleFailure.PriceRuleNotFound)
            : GetProductVariantPriceRuleResult.Succeeded(new PriceRuleSnapshot(rule, snapshot.ConcurrencyToken.Revision));
    }
}
