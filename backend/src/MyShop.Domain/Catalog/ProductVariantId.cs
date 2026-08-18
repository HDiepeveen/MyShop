namespace MyShop.Domain.Catalog;

public readonly record struct ProductVariantId
{
    private ProductVariantId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ProductVariantId New() => new(Guid.NewGuid());
}
