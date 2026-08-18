namespace MyShop.Domain.Catalog;

public sealed record ChoiceValue
{
    private ChoiceValue(string value) => Value = value;

    public string Value { get; }

    public static ChoiceValue Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Choice value must not be empty or whitespace.", nameof(value));
        return new ChoiceValue(value);
    }

    public override string ToString() => Value;
}
