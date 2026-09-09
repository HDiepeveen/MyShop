using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RemoveProductAttributeValue;

public sealed class RemoveProductAttributeValue
{
    private readonly IProductRepository _products;

    public RemoveProductAttributeValue(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<RemoveProductAttributeValueResult> ExecuteAsync(
        RemoveProductAttributeValueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.AttributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(command.AttributeDefinitionId));

        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return RemoveProductAttributeValueResult.Failed(RemoveProductAttributeValueFailure.ProductNotFound);

        if (!product.AttributeValues.Any(value => value.AttributeDefinitionId == command.AttributeDefinitionId))
            return RemoveProductAttributeValueResult.Succeeded;

        product.RemoveAttributeValue(command.AttributeDefinitionId);
        await _products.SaveAsync(product, cancellationToken);

        return RemoveProductAttributeValueResult.Succeeded;
    }
}
