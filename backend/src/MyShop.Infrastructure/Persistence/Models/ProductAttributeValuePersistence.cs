using MyShop.Domain.Catalog;

namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductAttributeValuePersistence
{
    public Guid ProductId { get; set; }
    public Guid AttributeDefinitionId { get; set; }
    public AttributeDataType DataType { get; set; }
    public int Ordinal { get; set; }
    public string? TextValue { get; set; }
    public long? IntegerValue { get; set; }
    public decimal? DecimalCoefficient { get; set; }
    public byte? DecimalScale { get; set; }
    public bool? BooleanValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public string? ChoiceValue { get; set; }
    public ProductPersistence Product { get; set; } = null!;
    public ICollection<ProductAttributeMultiChoiceValuePersistence> MultiChoiceValues { get; set; } = [];
}
