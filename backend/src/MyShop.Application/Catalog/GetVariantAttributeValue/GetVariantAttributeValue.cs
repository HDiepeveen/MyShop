using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetVariantAttributeValue;

public sealed class GetVariantAttributeValue
{
    private readonly IProductRepository _products;

    public GetVariantAttributeValue(IProductRepository products)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
    }

    public async Task<GetVariantAttributeValueResult> ExecuteAsync(
        GetVariantAttributeValueQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.ProductId == default)
            throw new ArgumentException("Product ID must not be empty.", nameof(query.ProductId));
        if (query.ProductVariantId == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(query.ProductVariantId));
        if (query.AttributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(query.AttributeDefinitionId));

        var snapshot = await _products.GetByIdAsync(query.ProductId, cancellationToken);
        if (snapshot is null)
            return GetVariantAttributeValueResult.Failed(GetVariantAttributeValueFailure.ProductNotFound);

        var variant = snapshot.Product.Variants.SingleOrDefault(candidate => candidate.Id == query.ProductVariantId);
        if (variant is null)
            return GetVariantAttributeValueResult.Failed(GetVariantAttributeValueFailure.VariantNotFound);
        var value = variant.AttributeValues.SingleOrDefault(candidate =>
            candidate.AttributeDefinitionId == query.AttributeDefinitionId);
        return value is null
            ? GetVariantAttributeValueResult.Failed(GetVariantAttributeValueFailure.AttributeValueNotFound)
            : GetVariantAttributeValueResult.Succeeded(new VariantAttributeValueSnapshot(
                value, snapshot.ConcurrencyToken.Revision));
    }
}
