using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.AddProductTypeAttribute;

public sealed class AddProductTypeAttribute
{
    private readonly IProductTypeRepository _productTypes;
    private readonly IProductTypeWriter _writer;

    public AddProductTypeAttribute(IProductTypeRepository productTypes, IProductTypeWriter writer)
    {
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<AttributeDefinition?> ExecuteAsync(
        AddProductTypeAttributeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(command.ProductTypeId));

        var productType = await _productTypes.GetByIdAsync(command.ProductTypeId, cancellationToken);
        if (productType is null)
            return null;

        var attribute = productType.AddAttribute(
            AttributeDefinitionId.New(), AttributeCode.Create(command.Code), command.DisplayName,
            command.DataType, command.IsRequired, command.IsFilterable, command.Scope);
        await _writer.SaveAsync(productType, cancellationToken);
        return attribute;
    }
}
