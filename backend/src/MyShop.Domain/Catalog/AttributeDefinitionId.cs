namespace MyShop.Domain.Catalog;

public readonly record struct AttributeDefinitionId
{
    private AttributeDefinitionId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AttributeDefinitionId New() => new(Guid.NewGuid());
}
