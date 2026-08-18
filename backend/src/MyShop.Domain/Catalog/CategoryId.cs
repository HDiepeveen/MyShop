namespace MyShop.Domain.Catalog;

public readonly record struct CategoryId
{
    private CategoryId(Guid value) => Value = value;

    public Guid Value { get; }

    public static CategoryId New() => new(Guid.NewGuid());
}
