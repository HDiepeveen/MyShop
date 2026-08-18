namespace MyShop.Domain.Catalog;

public readonly record struct ProductTypeId
{
    private ProductTypeId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static ProductTypeId New() => new(Guid.NewGuid());
}
