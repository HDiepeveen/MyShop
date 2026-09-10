using System.Collections.ObjectModel;

namespace MyShop.Domain.Catalog;

public sealed class ProductVariant
{
    private readonly List<AttributeValue> _attributeValues = [];
    private readonly ReadOnlyCollection<AttributeValue> _readOnlyAttributeValues;

    internal ProductVariant(ProductVariantId id, string name)
    {
        if (id == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(id));

        Id = id;
        Name = ValidateName(name);
        _readOnlyAttributeValues = _attributeValues.AsReadOnly();
    }

    private ProductVariant(
        ProductVariantId id,
        string name,
        Sku? sku,
        List<AttributeValue> attributeValues)
    {
        Id = id;
        Name = name;
        Sku = sku;
        _attributeValues.AddRange(attributeValues);
        _readOnlyAttributeValues = _attributeValues.AsReadOnly();
    }

    public ProductVariantId Id { get; }
    public string Name { get; private set; }
    public Sku? Sku { get; private set; }
    public IReadOnlyCollection<AttributeValue> AttributeValues => _readOnlyAttributeValues;

    internal static ProductVariant Rehydrate(
        ProductVariantId id,
        string name,
        Sku? sku,
        IEnumerable<AttributeValue> attributeValues)
    {
        ArgumentNullException.ThrowIfNull(attributeValues);

        var values = attributeValues.ToList();
        ValidateRehydratedValues(values);

        if (id == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(id));

        var validatedName = ValidateName(name);
        return new ProductVariant(id, validatedName, sku, values);
    }

    internal void Rename(string name) => Name = ValidateName(name);

    internal void SetSku(Sku sku)
    {
        ArgumentNullException.ThrowIfNull(sku);
        Sku = sku;
    }

    internal void SetAttributeValue(AttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var existingIndex = _attributeValues.FindIndex(existing =>
            existing.AttributeDefinitionId == value.AttributeDefinitionId);

        if (existingIndex >= 0)
            _attributeValues[existingIndex] = value;
        else
            _attributeValues.Add(value);
    }

    internal void RemoveAttributeValue(AttributeDefinitionId attributeDefinitionId)
    {
        if (attributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(attributeDefinitionId));

        var value = _attributeValues.SingleOrDefault(existing =>
            existing.AttributeDefinitionId == attributeDefinitionId)
            ?? throw new InvalidOperationException(
                $"Attribute value for definition ID '{attributeDefinitionId}' does not exist on this variant.");

        _attributeValues.Remove(value);
    }

    private static void ValidateRehydratedValues(List<AttributeValue> values)
    {
        if (values.Any(value => value is null))
            throw new InvalidOperationException("A product variant cannot contain null attribute values.");

        if (values.GroupBy(value => value.AttributeDefinitionId).Any(group => group.Count() > 1))
            throw new InvalidOperationException("A product variant cannot contain duplicate attribute definitions.");
    }

    private static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product variant name must not be empty or whitespace.", nameof(name));
        return name;
    }
}
