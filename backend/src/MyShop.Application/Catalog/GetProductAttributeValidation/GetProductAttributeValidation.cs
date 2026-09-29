using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GetProductAttributeValidation;

public sealed class GetProductAttributeValidation
{
    private readonly IProductRepository _products;
    private readonly IProductTypeRepository _productTypes;

    public GetProductAttributeValidation(IProductRepository products, IProductTypeRepository productTypes)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
    }

    public async Task<GetProductAttributeValidationResult> ExecuteAsync(
        GetProductAttributeValidationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(query.ProductId));

        var snapshot = await _products.GetByIdAsync(query.ProductId, cancellationToken);
        if (snapshot is null)
            return GetProductAttributeValidationResult.Failed(GetProductAttributeValidationFailure.ProductNotFound);
        var type = await _productTypes.GetByIdAsync(snapshot.Product.ProductTypeId, cancellationToken);
        if (type is null)
            return GetProductAttributeValidationResult.Failed(GetProductAttributeValidationFailure.ProductTypeNotFound);
        cancellationToken.ThrowIfCancellationRequested();
        return GetProductAttributeValidationResult.Succeeded(ProductAttributeValidation.Evaluate(snapshot.Product, type));
    }
}
