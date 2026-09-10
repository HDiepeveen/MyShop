namespace MyShop.Domain.Catalog;

public sealed record Sku
{
    public const int MaximumLength = 64;

    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Sku Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length is 0 or > MaximumLength)
            throw new ArgumentException($"SKU must contain between 1 and {MaximumLength} characters.", nameof(value));

        if (value.Any(char.IsWhiteSpace))
            throw new ArgumentException("SKU must not contain whitespace.", nameof(value));

        return new Sku(value.ToUpperInvariant());
    }

    public override string ToString() => Value;
}