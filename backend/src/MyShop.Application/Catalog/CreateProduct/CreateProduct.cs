using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.CreateProduct;

public sealed class CreateProduct
{
    private readonly IProductRepository _products;
    private readonly IProductTypeRepository _productTypes;

    public CreateProduct(
        IProductRepository products,
        IProductTypeRepository productTypes)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
    }

    public async Task<CreateProductResult> ExecuteAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ProductTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(command.ProductTypeId));

        if (await _productTypes.GetByIdAsync(command.ProductTypeId, cancellationToken) is null)
            return CreateProductResult.Failed(CreateProductFailure.ProductTypeNotFound);

        var product = Product.Create(command.Name, command.ProductTypeId, command.InitialVariantName);
        var token = await _products.AddAsync(product, cancellationToken);

        return CreateProductResult.Succeeded(new ProductSnapshot(product, token));
    }
}
