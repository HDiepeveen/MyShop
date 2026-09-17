using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProductType;

public sealed class GetProductType
{
    private readonly IProductTypeRepository _productTypes;

    public GetProductType(IProductTypeRepository productTypes) =>
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));

    public async Task<GetProductTypeResult> ExecuteAsync(
        GetProductTypeQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.ProductTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(query.ProductTypeId));

        var productType = await _productTypes.GetByIdAsync(query.ProductTypeId, cancellationToken);
        return productType is null
            ? GetProductTypeResult.Failed(GetProductTypeFailure.ProductTypeNotFound)
            : GetProductTypeResult.Succeeded(productType);
    }
}
