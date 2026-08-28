using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetProductAttributeValue;

public sealed class SetProductAttributeValue
{
    private readonly IProductRepository _products;
    private readonly IProductTypeRepository _productTypes;

    public SetProductAttributeValue(
        IProductRepository products,
        IProductTypeRepository productTypes)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
    }

    public async Task<SetProductAttributeValueResult> ExecuteAsync(
        SetProductAttributeValueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Value);
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return SetProductAttributeValueResult.Failed(SetProductAttributeValueFailure.ProductNotFound);

        var productType = await _productTypes.GetByIdAsync(product.ProductTypeId, cancellationToken);
        if (productType is null)
            return SetProductAttributeValueResult.Failed(SetProductAttributeValueFailure.ProductTypeNotFound);

        var definition = productType.AttributeDefinitions.SingleOrDefault(attribute =>
            attribute.Id == command.AttributeDefinitionId);
        if (definition is null)
            return SetProductAttributeValueResult.Failed(SetProductAttributeValueFailure.AttributeDefinitionNotFound);

        if (definition.Scope != AttributeScope.Product)
            return SetProductAttributeValueResult.Failed(SetProductAttributeValueFailure.WrongAttributeScope);

        if (definition.DataType != command.Value.DataType)
            return SetProductAttributeValueResult.Failed(SetProductAttributeValueFailure.WrongAttributeDataType);

        var value = CreateAttributeValue(command.AttributeDefinitionId, command.Value);
        product.SetAttributeValue(value);
        await _products.SaveAsync(product, cancellationToken);

        return SetProductAttributeValueResult.Succeeded;
    }

    private static AttributeValue CreateAttributeValue(
        AttributeDefinitionId attributeDefinitionId,
        SetProductAttributeValueInput value) => value switch
    {
        TextAttributeValueInput text => TextAttributeValue.Create(attributeDefinitionId, text.Value),
        IntegerAttributeValueInput integer => IntegerAttributeValue.Create(attributeDefinitionId, integer.Value),
        DecimalAttributeValueInput decimalValue => DecimalAttributeValue.Create(attributeDefinitionId, decimalValue.Value),
        BooleanAttributeValueInput boolean => BooleanAttributeValue.Create(attributeDefinitionId, boolean.Value),
        DateAttributeValueInput date => DateAttributeValue.Create(attributeDefinitionId, date.Value),
        ChoiceAttributeValueInput choice => ChoiceAttributeValue.Create(
            attributeDefinitionId,
            ChoiceValue.Create(choice.Value)),
        MultiChoiceAttributeValueInput multiChoice => MultiChoiceAttributeValue.Create(
            attributeDefinitionId,
            multiChoice.Values.Select(ChoiceValue.Create)),
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Attribute value input type is not supported.")
    };
}
