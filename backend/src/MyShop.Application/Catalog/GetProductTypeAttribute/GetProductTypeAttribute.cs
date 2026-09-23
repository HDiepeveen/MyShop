using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProductTypeAttribute;

public sealed class GetProductTypeAttribute
{
    private readonly IProductTypeRepository _productTypes;

    public GetProductTypeAttribute(IProductTypeRepository productTypes) =>
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));

    public async Task<GetProductTypeAttributeResult> ExecuteAsync(
        GetProductTypeAttributeQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.ProductTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(query.ProductTypeId));
        if (query.AttributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(query.AttributeDefinitionId));

        var productType = await _productTypes.GetByIdAsync(query.ProductTypeId, cancellationToken);
        if (productType is null)
            return GetProductTypeAttributeResult.Failed(GetProductTypeAttributeFailure.ProductTypeNotFound);
        var attribute = productType.AttributeDefinitions.SingleOrDefault(item => item.Id == query.AttributeDefinitionId);
        return attribute is null
            ? GetProductTypeAttributeResult.Failed(GetProductTypeAttributeFailure.AttributeNotFound)
            : GetProductTypeAttributeResult.Succeeded(attribute);
    }
}
