namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductTypePersistence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<AttributeDefinitionPersistence> AttributeDefinitions { get; set; } = [];
}