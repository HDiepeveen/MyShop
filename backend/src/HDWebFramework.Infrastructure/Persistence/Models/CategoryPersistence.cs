namespace HDWebFramework.Infrastructure.Persistence.Models;

internal sealed class CategoryPersistence
{
    public Guid Id { get; set; }
    public string? WebAddress { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public bool ShowInMenu { get; set; }
    public Guid NavigationRevision { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentCategoryId { get; set; }
    public CategoryPersistence? Parent { get; set; }
    public ICollection<CategoryPersistence> Children { get; set; } = [];
}