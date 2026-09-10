namespace MyShop.Domain.Catalog;

public readonly record struct ProductVariantId
{
    private ProductVariantId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ProductVariantId New() => new(Guid.NewGuid());

    internal static ProductVariantId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(value));

        return new ProductVariantId(value);
    }
}
