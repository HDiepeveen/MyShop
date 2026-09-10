using System.Collections.ObjectModel;

namespace MyShop.Domain.Catalog;

public sealed class ProductType
{
    private readonly List<AttributeDefinition> _attributeDefinitions = [];
    private readonly ReadOnlyCollection<AttributeDefinition> _readOnlyAttributeDefinitions;

    private ProductType(string name)
    {
        Id = ProductTypeId.New();
        Name = ValidateName(name);
        _readOnlyAttributeDefinitions = _attributeDefinitions.AsReadOnly();
    }

    private ProductType(ProductTypeId id, string name, List<AttributeDefinition> attributeDefinitions)
    {
        Id = id;
        Name = name;
        _attributeDefinitions.AddRange(attributeDefinitions);
        _readOnlyAttributeDefinitions = _attributeDefinitions.AsReadOnly();
    }

    public ProductTypeId Id { get; }
    public string Name { get; private set; }
    public IReadOnlyCollection<AttributeDefinition> AttributeDefinitions => _readOnlyAttributeDefinitions;

    public static ProductType Create(string name) => new(name);

    internal static ProductType Rehydrate(
        ProductTypeId id,
        string name,
        IEnumerable<AttributeDefinition> attributeDefinitions)
    {
        ArgumentNullException.ThrowIfNull(attributeDefinitions);

        var definitions = attributeDefinitions.ToList();
        if (id == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(id));
        if (definitions.Any(definition => definition is null))
            throw new InvalidOperationException("A product type cannot contain null attribute definitions.");
        if (definitions.GroupBy(definition => definition.Id).Any(group => group.Count() > 1))
            throw new InvalidOperationException("A product type cannot contain duplicate attribute definitions.");
        if (definitions.GroupBy(definition => definition.Code).Any(group => group.Count() > 1))
            throw new InvalidOperationException("A product type cannot contain duplicate attribute codes.");

        return new ProductType(id, ValidateName(name), definitions);
    }
    public void Rename(string name) => Name = ValidateName(name);

    public AttributeDefinition AddAttribute(AttributeDefinitionId id, AttributeCode code, string displayName,
        AttributeDataType dataType, bool isRequired, bool isFilterable, AttributeScope scope)
    {
        if (_attributeDefinitions.Any(attribute => attribute.Id == id))
            throw new InvalidOperationException($"An attribute with ID '{id}' already exists in this product type.");
        if (_attributeDefinitions.Any(attribute => attribute.Code == code))
            throw new InvalidOperationException($"An attribute with code '{code}' already exists in this product type.");

        var attribute = new AttributeDefinition(id, code, displayName, dataType, isRequired, isFilterable, scope);
        _attributeDefinitions.Add(attribute);
        return attribute;
    }

    public void RenameAttribute(AttributeDefinitionId attributeId, string displayName) => FindAttribute(attributeId).Rename(displayName);
    public void SetAttributeRequired(AttributeDefinitionId attributeId, bool required) => FindAttribute(attributeId).SetRequired(required);
    public void SetAttributeFilterable(AttributeDefinitionId attributeId, bool filterable) => FindAttribute(attributeId).SetFilterable(filterable);

    public void RemoveAttribute(AttributeDefinitionId attributeId)
    {
        var attribute = FindAttribute(attributeId);
        _attributeDefinitions.Remove(attribute);
    }

    private AttributeDefinition FindAttribute(AttributeDefinitionId attributeId) =>
        _attributeDefinitions.SingleOrDefault(attribute => attribute.Id == attributeId)
        ?? throw new InvalidOperationException($"Attribute with ID '{attributeId}' does not belong to this product type.");

    private static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product type name must not be empty or whitespace.", nameof(name));
        return name;
    }
}
