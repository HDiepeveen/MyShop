using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProductAttributeValue;

public sealed class GetProductAttributeValue
{
    private readonly IProductRepository _products;

    public GetProductAttributeValue(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<GetProductAttributeValueResult> ExecuteAsync(
        GetProductAttributeValueQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(query.ProductId));
        if (query.AttributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(query.AttributeDefinitionId));

        var snapshot = await _products.GetByIdAsync(query.ProductId, cancellationToken);
        if (snapshot is null)
            return GetProductAttributeValueResult.Failed(GetProductAttributeValueFailure.ProductNotFound);

        var value = snapshot.Product.AttributeValues.SingleOrDefault(candidate =>
            candidate.AttributeDefinitionId == query.AttributeDefinitionId);
        return value is null
            ? GetProductAttributeValueResult.Failed(GetProductAttributeValueFailure.AttributeValueNotFound)
            : GetProductAttributeValueResult.Succeeded(new ProductAttributeValueSnapshot(
                value, snapshot.ConcurrencyToken.Revision));
    }
}
