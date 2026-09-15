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

        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null)
            return SetProductAttributeValueResult.Failed(SetProductAttributeValueFailure.ProductNotFound);

        var product = snapshot.Product;

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

        var value = CatalogAttributeValueFactory.Create(command.AttributeDefinitionId, command.Value);
        product.SetAttributeValue(value);
        await _products.SaveAsync(product, snapshot.ConcurrencyToken, cancellationToken);

        return SetProductAttributeValueResult.Succeeded;
    }
}
