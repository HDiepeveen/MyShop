using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RemoveProductTypeAttribute;

public sealed class RemoveProductTypeAttribute
{
    private readonly IProductTypeRepository _productTypes;
    private readonly IProductTypeWriter _writer;

    public RemoveProductTypeAttribute(IProductTypeRepository productTypes, IProductTypeWriter writer)
    {
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<ProductTypeAttributeUpdateResult> ExecuteAsync(
        RemoveProductTypeAttributeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductTypeId == default || command.AttributeDefinitionId == default)
            throw new ArgumentException("Product type and attribute IDs must not be empty.", nameof(command));

        var productType = await _productTypes.GetByIdAsync(command.ProductTypeId, cancellationToken);
        if (productType is null)
            return ProductTypeAttributeUpdateResult.ProductTypeNotFound;
        if (!productType.AttributeDefinitions.Any(x => x.Id == command.AttributeDefinitionId))
            return ProductTypeAttributeUpdateResult.AttributeNotFound;

        productType.RemoveAttribute(command.AttributeDefinitionId);
        await _writer.SaveAsync(productType, cancellationToken);
        return ProductTypeAttributeUpdateResult.Succeeded;
    }
}
