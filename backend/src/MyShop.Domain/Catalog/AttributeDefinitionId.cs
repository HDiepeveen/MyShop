namespace MyShop.Domain.Catalog;

public readonly record struct AttributeDefinitionId
{
    private AttributeDefinitionId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AttributeDefinitionId New() => new(Guid.NewGuid());

    internal static AttributeDefinitionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(value));

        return new AttributeDefinitionId(value);
    }
}
