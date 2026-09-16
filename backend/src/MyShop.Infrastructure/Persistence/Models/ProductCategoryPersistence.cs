namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductCategoryPersistence
{
    public Guid ProductId { get; set; }
    public Guid CategoryId { get; set; }
    public int Ordinal { get; set; }
    public ProductPersistence Product { get; set; } = null!;
}
