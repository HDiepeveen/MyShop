using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.CreateProductType;

public sealed class CreateProductType
{
    private readonly IProductTypeWriter _writer;

    public CreateProductType(IProductTypeWriter writer) =>
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public async Task<ProductType> ExecuteAsync(
        CreateProductTypeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var productType = ProductType.Create(command.Name);
        await _writer.AddAsync(productType, cancellationToken);
        return productType;
    }
}
