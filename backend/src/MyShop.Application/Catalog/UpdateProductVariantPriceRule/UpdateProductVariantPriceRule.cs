using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.UpdateProductVariantPriceRule;

public sealed class UpdateProductVariantPriceRule
{
    private readonly IProductRepository _products;

    public UpdateProductVariantPriceRule(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<UpdateProductVariantPriceRuleResult> ExecuteAsync(
        UpdateProductVariantPriceRuleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));
        if (command.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(command.ProductVariantId));

        if (command.PriceRuleId == Guid.Empty)
            throw new ArgumentException("Price rule ID must not be empty.", nameof(command.PriceRuleId));

        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null)
            return UpdateProductVariantPriceRuleResult.Failed(UpdateProductVariantPriceRuleFailure.ProductNotFound);

        var variant = snapshot.Product.Variants.SingleOrDefault(candidate =>
            candidate.Id == command.ProductVariantId);
        if (variant is null)
            return UpdateProductVariantPriceRuleResult.Failed(UpdateProductVariantPriceRuleFailure.VariantNotFound);

        if (!variant.PriceRules.Any(rule => rule.Id == command.PriceRuleId))
            return UpdateProductVariantPriceRuleResult.Failed(UpdateProductVariantPriceRuleFailure.PriceRuleNotFound);

        var changed = snapshot.Product.UpdateVariantPriceRule(command.ProductVariantId, command.PriceRuleId,
            command.Name, command.AdjustmentType, command.Value, command.Priority, command.StartsAt, command.EndsAt);
        if (changed)
            await _products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);

        return UpdateProductVariantPriceRuleResult.Succeeded;
    }
}
