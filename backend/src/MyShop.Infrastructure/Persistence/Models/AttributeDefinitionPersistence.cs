using MyShop.Domain.Catalog;

namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class AttributeDefinitionPersistence
{
    public Guid Id { get; set; }
    public Guid ProductTypeId { get; set; }
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public AttributeDataType DataType { get; set; }
    public AttributeScope Scope { get; set; }
    public bool IsRequired { get; set; }
    public bool IsFilterable { get; set; }
    public ProductTypePersistence ProductType { get; set; } = null!;
}