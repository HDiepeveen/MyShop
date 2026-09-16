namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductPersistence
{
    public Guid Id { get; set; }
    public Guid ProductTypeId { get; set; }
    public string Name { get; set; } = null!;
    public Guid Version { get; set; }
    public ProductTypePersistence ProductType { get; set; } = null!;
    public ICollection<ProductVariantPersistence> Variants { get; set; } = [];
    public ICollection<ProductCategoryPersistence> Categories { get; set; } = [];
    public ICollection<ProductAttributeValuePersistence> AttributeValues { get; set; } = [];
}
