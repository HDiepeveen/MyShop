namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductAttributeMultiChoiceValuePersistence
{
    public Guid ProductId { get; set; }
    public Guid AttributeDefinitionId { get; set; }
    public int Ordinal { get; set; }
    public string Value { get; set; } = null!;
    public ProductAttributeValuePersistence AttributeValue { get; set; } = null!;
}
