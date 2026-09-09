using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RemoveVariantAttributeValue;

public sealed class RemoveVariantAttributeValue
{
    private readonly IProductRepository _products;

    public RemoveVariantAttributeValue(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<RemoveVariantAttributeValueResult> ExecuteAsync(
        RemoveVariantAttributeValueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.AttributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(command.AttributeDefinitionId));

        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return RemoveVariantAttributeValueResult.Failed(RemoveVariantAttributeValueFailure.ProductNotFound);

        var variant = product.Variants.SingleOrDefault(candidate => candidate.Id == command.ProductVariantId);
        if (variant is null)
            return RemoveVariantAttributeValueResult.Failed(RemoveVariantAttributeValueFailure.VariantNotFound);

        if (!variant.AttributeValues.Any(value => value.AttributeDefinitionId == command.AttributeDefinitionId))
            return RemoveVariantAttributeValueResult.Succeeded;

        product.RemoveVariantAttributeValue(command.ProductVariantId, command.AttributeDefinitionId);
        await _products.SaveAsync(product, cancellationToken);

        return RemoveVariantAttributeValueResult.Succeeded;
    }
}
