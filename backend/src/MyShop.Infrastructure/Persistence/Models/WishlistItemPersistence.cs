namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class WishlistItemPersistence
{
    public string UserId { get; set; } = null!;
    public Guid ProductId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public ProductPersistence Product { get; set; } = null!;
}
