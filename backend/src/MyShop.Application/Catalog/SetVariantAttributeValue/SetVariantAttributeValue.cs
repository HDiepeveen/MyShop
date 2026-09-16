using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetVariantAttributeValue;

public sealed class SetVariantAttributeValue
{
    private readonly IProductRepository _products;
    private readonly IProductTypeRepository _productTypes;

    public SetVariantAttributeValue(
        IProductRepository products,
        IProductTypeRepository productTypes)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
    }

    public async Task<SetVariantAttributeValueResult> ExecuteAsync(
        SetVariantAttributeValueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Value);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));
        if (command.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(command.ProductVariantId));
        if (command.AttributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(command.AttributeDefinitionId));

        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null)
            return SetVariantAttributeValueResult.Failed(SetVariantAttributeValueFailure.ProductNotFound);

        var product = snapshot.Product;

        if (!product.Variants.Any(variant => variant.Id == command.ProductVariantId))
            return SetVariantAttributeValueResult.Failed(SetVariantAttributeValueFailure.VariantNotFound);

        var productType = await _productTypes.GetByIdAsync(product.ProductTypeId, cancellationToken);
        if (productType is null)
            return SetVariantAttributeValueResult.Failed(SetVariantAttributeValueFailure.ProductTypeNotFound);

        var definition = productType.AttributeDefinitions.SingleOrDefault(attribute =>
            attribute.Id == command.AttributeDefinitionId);
        if (definition is null)
            return SetVariantAttributeValueResult.Failed(SetVariantAttributeValueFailure.AttributeDefinitionNotFound);

        if (definition.Scope != AttributeScope.Variant)
            return SetVariantAttributeValueResult.Failed(SetVariantAttributeValueFailure.WrongAttributeScope);

        if (definition.DataType != command.Value.DataType)
            return SetVariantAttributeValueResult.Failed(SetVariantAttributeValueFailure.WrongAttributeDataType);

        var value = CatalogAttributeValueFactory.Create(command.AttributeDefinitionId, command.Value);
        product.SetVariantAttributeValue(command.ProductVariantId, value);
        await _products.SaveAsync(product, snapshot.ConcurrencyToken, cancellationToken);

        return SetVariantAttributeValueResult.Succeeded;
    }
}
