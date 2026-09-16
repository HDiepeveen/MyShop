namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductVariantAttributeMultiChoiceValuePersistence
{
    public Guid ProductVariantId { get; set; }
    public Guid AttributeDefinitionId { get; set; }
    public int Ordinal { get; set; }
    public string Value { get; set; } = null!;
    public ProductVariantAttributeValuePersistence AttributeValue { get; set; } = null!;
}
