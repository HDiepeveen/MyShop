namespace MyShop.Domain.Catalog;

public readonly record struct CategoryId
{
    private CategoryId(Guid value) => Value = value;

    public Guid Value { get; }

    public static CategoryId New() => new(Guid.NewGuid());

    internal static CategoryId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Category ID must not be empty.", nameof(value));

        return new CategoryId(value);
    }
}
