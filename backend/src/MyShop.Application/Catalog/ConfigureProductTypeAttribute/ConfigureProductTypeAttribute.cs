using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ConfigureProductTypeAttribute;

public sealed class ConfigureProductTypeAttribute
{
    private readonly IProductTypeRepository _productTypes;
    private readonly IProductTypeWriter _writer;

    public ConfigureProductTypeAttribute(IProductTypeRepository productTypes, IProductTypeWriter writer)
    {
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<ProductTypeAttributeUpdateResult> ExecuteAsync(
        ConfigureProductTypeAttributeCommand command, CancellationToken cancellationToken)
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
        if (attribute.IsRequired == command.IsRequired && attribute.IsFilterable == command.IsFilterable)
            return ProductTypeAttributeUpdateResult.Succeeded;

        productType.SetAttributeRequired(command.AttributeDefinitionId, command.IsRequired);
        productType.SetAttributeFilterable(command.AttributeDefinitionId, command.IsFilterable);
        await _writer.SaveAsync(productType, cancellationToken);
        return ProductTypeAttributeUpdateResult.Succeeded;
    }
}
