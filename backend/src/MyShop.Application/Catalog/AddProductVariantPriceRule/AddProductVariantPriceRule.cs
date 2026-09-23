using MyShop.Domain.Catalog;
using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.AddProductVariantPriceRule;

public sealed class AddProductVariantPriceRule
{
    private readonly IProductRepository _products;

    public AddProductVariantPriceRule(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<AddProductVariantPriceRuleResult> ExecuteAsync(
        AddProductVariantPriceRuleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));
        if (command.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(command.ProductVariantId));

        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null)
            return AddProductVariantPriceRuleResult.Failed(AddProductVariantPriceRuleFailure.ProductNotFound);

        var variant = snapshot.Product.Variants.SingleOrDefault(candidate =>
            candidate.Id == command.ProductVariantId);
        if (variant is null)
            return AddProductVariantPriceRuleResult.Failed(AddProductVariantPriceRuleFailure.VariantNotFound);

        var rule = PriceRule.Create(command.Name, command.AdjustmentType, command.Value,
            command.Priority, command.StartsAt, command.EndsAt);
        snapshot.Product.AddVariantPriceRule(command.ProductVariantId, rule);
        await _products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);

        return AddProductVariantPriceRuleResult.Succeeded(rule.Id);
    }
}
