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

    public ProductVariantId Id { get; }
    public string Name { get; private set; }
    public Sku? Sku { get; private set; }
    public IReadOnlyCollection<AttributeValue> AttributeValues => _readOnlyAttributeValues;

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

    private static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product variant name must not be empty or whitespace.", nameof(name));
        return name;
    }
}
