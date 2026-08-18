using System.Collections.ObjectModel;

namespace MyShop.Domain.Catalog;

public abstract record AttributeValue
{
    private protected AttributeValue(AttributeDefinitionId attributeDefinitionId)
    {
        if (attributeDefinitionId == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(attributeDefinitionId));

        AttributeDefinitionId = attributeDefinitionId;
    }

    public AttributeDefinitionId AttributeDefinitionId { get; }
    public abstract AttributeDataType DataType { get; }
}

public sealed record TextAttributeValue : AttributeValue
{
    private TextAttributeValue(AttributeDefinitionId attributeDefinitionId, string value)
        : base(attributeDefinitionId) => Value = ValidateValue(value);

    public string Value { get; }
    public override AttributeDataType DataType => AttributeDataType.Text;

    public static TextAttributeValue Create(AttributeDefinitionId attributeDefinitionId, string value) =>
        new(attributeDefinitionId, value);

    private static string ValidateValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Text attribute value must not be empty or whitespace.", nameof(value));
        return value;
    }
}

public sealed record IntegerAttributeValue : AttributeValue
{
    private IntegerAttributeValue(AttributeDefinitionId attributeDefinitionId, long value)
        : base(attributeDefinitionId) => Value = value;

    public long Value { get; }
    public override AttributeDataType DataType => AttributeDataType.Integer;

    public static IntegerAttributeValue Create(AttributeDefinitionId attributeDefinitionId, long value) =>
        new(attributeDefinitionId, value);
}

public sealed record DecimalAttributeValue : AttributeValue
{
    private DecimalAttributeValue(AttributeDefinitionId attributeDefinitionId, decimal value)
        : base(attributeDefinitionId) => Value = value;

    public decimal Value { get; }
    public override AttributeDataType DataType => AttributeDataType.Decimal;

    public static DecimalAttributeValue Create(AttributeDefinitionId attributeDefinitionId, decimal value) =>
        new(attributeDefinitionId, value);
}

public sealed record BooleanAttributeValue : AttributeValue
{
    private BooleanAttributeValue(AttributeDefinitionId attributeDefinitionId, bool value)
        : base(attributeDefinitionId) => Value = value;

    public bool Value { get; }
    public override AttributeDataType DataType => AttributeDataType.Boolean;

    public static BooleanAttributeValue Create(AttributeDefinitionId attributeDefinitionId, bool value) =>
        new(attributeDefinitionId, value);
}

public sealed record DateAttributeValue : AttributeValue
{
    private DateAttributeValue(AttributeDefinitionId attributeDefinitionId, DateOnly value)
        : base(attributeDefinitionId) => Value = value;

    public DateOnly Value { get; }
    public override AttributeDataType DataType => AttributeDataType.Date;

    public static DateAttributeValue Create(AttributeDefinitionId attributeDefinitionId, DateOnly value) =>
        new(attributeDefinitionId, value);
}

public sealed record ChoiceAttributeValue : AttributeValue
{
    private ChoiceAttributeValue(AttributeDefinitionId attributeDefinitionId, ChoiceValue value)
        : base(attributeDefinitionId) => Value = value ?? throw new ArgumentNullException(nameof(value));

    public ChoiceValue Value { get; }
    public override AttributeDataType DataType => AttributeDataType.Choice;

    public static ChoiceAttributeValue Create(AttributeDefinitionId attributeDefinitionId, ChoiceValue value) =>
        new(attributeDefinitionId, value);
}

public sealed record MultiChoiceAttributeValue : AttributeValue
{
    private readonly ReadOnlyCollection<ChoiceValue> _values;

    private MultiChoiceAttributeValue(AttributeDefinitionId attributeDefinitionId, IEnumerable<ChoiceValue> values)
        : base(attributeDefinitionId)
    {
        ArgumentNullException.ThrowIfNull(values);

        var copiedValues = values.ToList();
        if (copiedValues.Count == 0)
            throw new ArgumentException("Multi-choice attribute value must contain at least one selection.", nameof(values));
        if (copiedValues.Any(value => value is null))
            throw new ArgumentException("Multi-choice attribute value must not contain null selections.", nameof(values));
        if (copiedValues.Distinct().Count() != copiedValues.Count)
            throw new ArgumentException("Multi-choice attribute value must not contain duplicate selections.", nameof(values));

        _values = copiedValues.AsReadOnly();
    }

    public IReadOnlyList<ChoiceValue> Values => _values;
    public override AttributeDataType DataType => AttributeDataType.MultiChoice;

    public static MultiChoiceAttributeValue Create(
        AttributeDefinitionId attributeDefinitionId,
        IEnumerable<ChoiceValue> values) => new(attributeDefinitionId, values);

    public bool Equals(MultiChoiceAttributeValue? other) =>
        other is not null &&
        AttributeDefinitionId == other.AttributeDefinitionId &&
        _values.SequenceEqual(other._values);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(AttributeDefinitionId);
        foreach (var value in _values)
            hashCode.Add(value);
        return hashCode.ToHashCode();
    }
}
