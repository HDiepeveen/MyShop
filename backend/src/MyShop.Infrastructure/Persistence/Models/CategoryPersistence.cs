namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class CategoryPersistence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentCategoryId { get; set; }
    public CategoryPersistence? Parent { get; set; }
    public ICollection<CategoryPersistence> Children { get; set; } = [];
}