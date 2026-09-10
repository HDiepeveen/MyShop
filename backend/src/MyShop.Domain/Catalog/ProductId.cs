namespace MyShop.Domain.Catalog;

public readonly record struct ProductId
{
    private ProductId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ProductId New() => new(Guid.NewGuid());

    internal static ProductId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Product ID must not be empty.", nameof(value));

        return new ProductId(value);
    }
}
