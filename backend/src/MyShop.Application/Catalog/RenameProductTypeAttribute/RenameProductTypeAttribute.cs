using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RenameProductTypeAttribute;

public sealed class RenameProductTypeAttribute
{
    private readonly IProductTypeRepository _productTypes;
    private readonly IProductTypeWriter _writer;

    public RenameProductTypeAttribute(IProductTypeRepository productTypes, IProductTypeWriter writer)
    {
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<ProductTypeAttributeUpdateResult> ExecuteAsync(
        RenameProductTypeAttributeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductTypeId == default || command.AttributeDefinitionId == default)
            throw new ArgumentException("Product type and attribute IDs must not be empty.", nameof(command));

        var productType = await _productTypes.GetByIdAsync(command.ProductTypeId, cancellationToken);
        if (productType is null)
            return ProductTypeAttributeUpdateResult.ProductTypeNotFound;
        var attribute = productType.AttributeDefinitions.SingleOrDefault(x => x.Id == command.AttributeDefinitionId);
        if (attribute is null)
            return ProductTypeAttributeUpdateResult.AttributeNotFound;
        if (attribute.DisplayName == command.DisplayName)
            return ProductTypeAttributeUpdateResult.Succeeded;

        productType.RenameAttribute(command.AttributeDefinitionId, command.DisplayName);
        await _writer.SaveAsync(productType, cancellationToken);
        return ProductTypeAttributeUpdateResult.Succeeded;
    }
}
