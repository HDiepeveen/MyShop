namespace MyShop.Application.Customers.Abstractions;

public enum WishlistSort { Newest, Name }

public sealed record WishlistItem(Guid ProductId, string Name, string? ImageUrl, string ImageAlt,
    bool IsAvailable, DateTimeOffset AddedAt);

public sealed record WishlistPage(IReadOnlyList<WishlistItem> Items, int TotalCount);

public interface IWishlistRepository
{
    Task<WishlistPage> ListAsync(string userId, int offset, int limit, CancellationToken cancellationToken, string? search = null, WishlistSort sort = WishlistSort.Newest);
    Task<bool> ContainsAsync(string userId, Guid productId, CancellationToken cancellationToken);
    Task<bool> AddAsync(string userId, Guid productId, DateTimeOffset addedAt, CancellationToken cancellationToken);
    Task RemoveAsync(string userId, Guid productId, CancellationToken cancellationToken);
}
