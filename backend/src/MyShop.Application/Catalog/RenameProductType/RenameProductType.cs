using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RenameProductType;

public sealed class RenameProductType
{
    private readonly IProductTypeRepository _productTypes;
    private readonly IProductTypeWriter _writer;

    public RenameProductType(IProductTypeRepository productTypes, IProductTypeWriter writer)
    {
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<bool> ExecuteAsync(
        RenameProductTypeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(command.ProductTypeId));

        var productType = await _productTypes.GetByIdAsync(command.ProductTypeId, cancellationToken);
        if (productType is null)
            return false;
        if (productType.Name == command.Name)
            return true;

        productType.Rename(command.Name);
        await _writer.SaveAsync(productType, cancellationToken);
        return true;
    }
}
