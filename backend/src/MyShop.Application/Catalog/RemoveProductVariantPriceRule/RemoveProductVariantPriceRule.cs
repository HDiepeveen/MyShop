using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RemoveProductVariantPriceRule;

public sealed class RemoveProductVariantPriceRule
{
    private readonly IProductRepository _products;

    public RemoveProductVariantPriceRule(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<RemoveProductVariantPriceRuleResult> ExecuteAsync(
        RemoveProductVariantPriceRuleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));
        if (command.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(command.ProductVariantId));
        if (command.PriceRuleId == default)
            throw new ArgumentException("Price rule ID must not be empty.", nameof(command.PriceRuleId));

        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null)
            return RemoveProductVariantPriceRuleResult.Failed(RemoveProductVariantPriceRuleFailure.ProductNotFound);

        var product = snapshot.Product;

        var variant = product.Variants.SingleOrDefault(candidate => candidate.Id == command.ProductVariantId);
        if (variant is null)
            return RemoveProductVariantPriceRuleResult.Failed(RemoveProductVariantPriceRuleFailure.VariantNotFound);

        if (!variant.PriceRules.Any(rule => rule.Id == command.PriceRuleId))
            return RemoveProductVariantPriceRuleResult.Succeeded;

        product.RemoveVariantPriceRule(command.ProductVariantId, command.PriceRuleId);
        await _products.SaveAsync(product, snapshot.ConcurrencyToken, cancellationToken);

        return RemoveProductVariantPriceRuleResult.Succeeded;
    }
}
