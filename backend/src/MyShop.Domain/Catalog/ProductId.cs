namespace MyShop.Domain.Catalog;

public readonly record struct ProductId
{
    private ProductId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ProductId New() => new(Guid.NewGuid());
}
