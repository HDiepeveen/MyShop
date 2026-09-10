namespace MyShop.Domain.Catalog;

public readonly record struct ProductTypeId
{
    private ProductTypeId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static ProductTypeId New() => new(Guid.NewGuid());

    internal static ProductTypeId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Product type ID must not be empty.", nameof(value));

        return new ProductTypeId(value);
    }
}
