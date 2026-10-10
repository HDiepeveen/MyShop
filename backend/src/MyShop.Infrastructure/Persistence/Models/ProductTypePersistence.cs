namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductTypePersistence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? AboutHeading { get; set; }
    public string? AttributesHeading { get; set; }
    public Guid SectionHeadingsRevision { get; set; }
    public ICollection<AttributeDefinitionPersistence> AttributeDefinitions { get; set; } = [];
}