using System.Collections.ObjectModel;

namespace MyShop.Domain.Catalog;

public sealed class ProductVariant
{
    private readonly List<AttributeValue> _attributeValues = [];
    private readonly ReadOnlyCollection<AttributeValue> _readOnlyAttributeValues;
    private readonly List<PriceRule> _priceRules = [];
    private readonly ReadOnlyCollection<PriceRule> _readOnlyPriceRules;

    internal ProductVariant(ProductVariantId id, string name)
    {
        if (id == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(id));

        Id = id;
        Name = ValidateName(name);
        _readOnlyAttributeValues = _attributeValues.AsReadOnly();
        _readOnlyPriceRules = _priceRules.AsReadOnly();
    }

    private ProductVariant(
        ProductVariantId id,
        string name,
        Sku? sku,
        List<AttributeValue> attributeValues,
        Money? price,
        List<PriceRule> priceRules)
    {
        Id = id;
        Name = name;
        Sku = sku;
        Price = price;
        _attributeValues.AddRange(attributeValues);
        _readOnlyAttributeValues = _attributeValues.AsReadOnly();
        _priceRules.AddRange(priceRules);
        _readOnlyPriceRules = _priceRules.AsReadOnly();
    }

    public ProductVariantId Id { get; }
    public string Name { get; private set; }
    public Money? Price { get; private set; }
    public IReadOnlyCollection<PriceRule> PriceRules => _readOnlyPriceRules;
    public Sku? Sku { get; private set; }
    public IReadOnlyCollection<AttributeValue> AttributeValues => _readOnlyAttributeValues;

    internal static ProductVariant Rehydrate(
        ProductVariantId id,
        string name,
        Sku? sku,
        IEnumerable<AttributeValue> attributeValues,
        Money? price = null,
        IEnumerable<PriceRule>? priceRules = null)
    {
        ArgumentNullException.ThrowIfNull(attributeValues);

        var values = attributeValues.ToList();
        ValidateRehydratedValues(values);

        if (id == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(id));

        var validatedName = ValidateName(name);
        var rules = (priceRules ?? []).ToList();
        if (rules.Any(rule => rule is null)) throw new InvalidOperationException("A variant cannot contain null price rules.");
        if (rules.GroupBy(rule => rule.Id).Any(group => group.Count() > 1)) throw new InvalidOperationException("A variant cannot contain duplicate price rules.");
        return new ProductVariant(id, validatedName, sku, values, price, rules);
    }

    internal void Rename(string name) => Name = ValidateName(name);

    internal void SetSku(Sku sku)
    {
        ArgumentNullException.ThrowIfNull(sku);
        Sku = sku;
    }

    internal void ClearSku() => Sku = null;

    internal void SetPrice(Money price) => Price = price;

    internal void ClearPrice() => Price = null;

    internal void AddPriceRule(PriceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (_priceRules.Any(existing => existing.Id == rule.Id)) throw new InvalidOperationException("This price rule is already assigned.");
        _priceRules.Add(rule);
    }

    internal void RemovePriceRule(Guid ruleId)
    {
        var rule = _priceRules.SingleOrDefault(candidate => candidate.Id == ruleId)
            ?? throw new InvalidOperationException("The price rule is not assigned to this variant.");
        _priceRules.Remove(rule);
    }

    public Money CalculatePrice(DateTimeOffset at)
    {
        var basePrice = Price ?? throw new InvalidOperationException("A base price must be set before calculating a price.");
        var rule = _priceRules.Where(candidate => candidate.IsActiveAt(at))
            .OrderByDescending(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefault();
        return rule?.Apply(basePrice) ?? basePrice;
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
