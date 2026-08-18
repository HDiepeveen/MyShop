using System.Collections.ObjectModel;

namespace MyShop.Domain.Catalog;

public sealed class Product
{
    private readonly List<ProductVariant> _variants = [];
    private readonly ReadOnlyCollection<ProductVariant> _readOnlyVariants;
    private readonly List<AttributeValue> _attributeValues = [];
    private readonly ReadOnlyCollection<AttributeValue> _readOnlyAttributeValues;

    private Product(string name, ProductTypeId productTypeId, string initialVariantName)
    {
        if (productTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(productTypeId));

        Id = ProductId.New();
        ProductTypeId = productTypeId;
        Name = ValidateName(name);
        _readOnlyVariants = _variants.AsReadOnly();
        _readOnlyAttributeValues = _attributeValues.AsReadOnly();
        AddVariant(initialVariantName);
    }

    public ProductId Id { get; }
    public ProductTypeId ProductTypeId { get; }
    public string Name { get; private set; }
    public IReadOnlyCollection<ProductVariant> Variants => _readOnlyVariants;
    public IReadOnlyCollection<AttributeValue> AttributeValues => _readOnlyAttributeValues;

    public static Product Create(string name, ProductTypeId productTypeId, string initialVariantName) =>
        new(name, productTypeId, initialVariantName);

    public void Rename(string name) => Name = ValidateName(name);

    public ProductVariant AddVariant(string name)
    {
        var variant = new ProductVariant(ProductVariantId.New(), name);
        _variants.Add(variant);
        return variant;
    }

    public void RenameVariant(ProductVariantId variantId, string name) => FindVariant(variantId).Rename(name);

    public void SetAttributeValue(AttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        SetAttributeValue(_attributeValues, value);
    }

    public void RemoveAttributeValue(AttributeDefinitionId attributeDefinitionId) =>
        RemoveAttributeValue(_attributeValues, attributeDefinitionId);

    public void SetVariantAttributeValue(ProductVariantId variantId, AttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        FindVariant(variantId).SetAttributeValue(value);
    }

    public void RemoveVariantAttributeValue(
        ProductVariantId variantId,
        AttributeDefinitionId attributeDefinitionId) =>
        FindVariant(variantId).RemoveAttributeValue(attributeDefinitionId);

    public void RemoveVariant(ProductVariantId variantId)
    {
        var variant = FindVariant(variantId);

        if (_variants.Count == 1)
            throw new InvalidOperationException("A product must contain at least one variant.");

        _variants.Remove(variant);
    }

    private ProductVariant FindVariant(ProductVariantId variantId) =>
        _variants.SingleOrDefault(variant => variant.Id == variantId)
        ?? throw new InvalidOperationException($"Variant with ID '{variantId}' does not belong to this product.");

    private static void SetAttributeValue(List<AttributeValue> values, AttributeValue value)
    {
        var existingIndex = values.FindIndex(existing =>
            existing.AttributeDefinitionId == value.AttributeDefinitionId);

        if (existingIndex >= 0)
            values[existingIndex] = value;
        else
            values.Add(value);
    }

    private static void RemoveAttributeValue(
        List<AttributeValue> values,
        AttributeDefinitionId attributeDefinitionId)
    {
        if (attributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(attributeDefinitionId));

        var value = values.SingleOrDefault(existing =>
            existing.AttributeDefinitionId == attributeDefinitionId)
            ?? throw new InvalidOperationException(
                $"Attribute value for definition ID '{attributeDefinitionId}' does not exist.");

        values.Remove(value);
    }

    private static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name must not be empty or whitespace.", nameof(name));
        return name;
    }
}
