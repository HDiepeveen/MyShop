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

        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return SetVariantAttributeValueResult.Failed(SetVariantAttributeValueFailure.ProductNotFound);

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
        await _products.SaveAsync(product, cancellationToken);

        return SetVariantAttributeValueResult.Succeeded;
    }
}
