using System.Collections.ObjectModel;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetProductAttributeValue;

public abstract record SetProductAttributeValueInput
{
    public abstract AttributeDataType DataType { get; }
}

public sealed record TextAttributeValueInput(string Value) : SetProductAttributeValueInput
{
    public override AttributeDataType DataType => AttributeDataType.Text;
}

public sealed record IntegerAttributeValueInput(long Value) : SetProductAttributeValueInput
{
    public override AttributeDataType DataType => AttributeDataType.Integer;
}

public sealed record DecimalAttributeValueInput(decimal Value) : SetProductAttributeValueInput
{
    public override AttributeDataType DataType => AttributeDataType.Decimal;
}

public sealed record BooleanAttributeValueInput(bool Value) : SetProductAttributeValueInput
{
    public override AttributeDataType DataType => AttributeDataType.Boolean;
}

public sealed record DateAttributeValueInput(DateOnly Value) : SetProductAttributeValueInput
{
    public override AttributeDataType DataType => AttributeDataType.Date;
}

public sealed record ChoiceAttributeValueInput(string Value) : SetProductAttributeValueInput
{
    public override AttributeDataType DataType => AttributeDataType.Choice;
}

public sealed record MultiChoiceAttributeValueInput : SetProductAttributeValueInput
{
    private readonly ReadOnlyCollection<string> _values;

    public MultiChoiceAttributeValueInput(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values.ToList().AsReadOnly();
    }

    public IReadOnlyList<string> Values => _values;
    public override AttributeDataType DataType => AttributeDataType.MultiChoice;
}
