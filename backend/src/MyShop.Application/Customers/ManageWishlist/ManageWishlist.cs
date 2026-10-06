using MyShop.Application.Customers.Abstractions;

namespace MyShop.Application.Customers.ManageWishlist;

public sealed record ListWishlistQuery(string UserId, int Offset = 0, int Limit = ListWishlist.DefaultLimit, string? Search = null, WishlistSort Sort = WishlistSort.Newest);

public sealed class ListWishlist(IWishlistRepository repository)
{
    public const int DefaultLimit = 20;
    public const int MaximumLimit = 100;
    public Task<WishlistPage> ExecuteAsync(ListWishlistQuery query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.UserId);
        if (query.Offset < 0) throw new ArgumentOutOfRangeException(nameof(query.Offset));
        if (query.Limit is < 1 or > MaximumLimit) throw new ArgumentOutOfRangeException(nameof(query.Limit));
        if (!Enum.IsDefined(query.Sort)) throw new ArgumentOutOfRangeException(nameof(query.Sort));
        var search = query.Search?.Trim();
        if (search?.Length > 200) throw new ArgumentException("Search must not exceed 200 characters.", nameof(query));
        return repository.ListAsync(query.UserId, query.Offset, query.Limit, cancellationToken,
            string.IsNullOrEmpty(search) ? null : search, query.Sort);
    }
}

public sealed class GetWishlistState(IWishlistRepository repository)
{
    public Task<bool> ExecuteAsync(string userId, Guid productId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (productId == Guid.Empty) throw new ArgumentException("Een product is verplicht.", nameof(productId));
        return repository.ContainsAsync(userId, productId, cancellationToken);
    }
}

public sealed class AddWishlistItem(IWishlistRepository repository, TimeProvider timeProvider)
{
    public Task<bool> ExecuteAsync(string userId, Guid productId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (productId == Guid.Empty) throw new ArgumentException("Een product is verplicht.", nameof(productId));
        return repository.AddAsync(userId, productId, timeProvider.GetUtcNow(), cancellationToken);
    }
}

public sealed class RemoveWishlistItem(IWishlistRepository repository)
{
    public Task ExecuteAsync(string userId, Guid productId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (productId == Guid.Empty) throw new ArgumentException("Een product is verplicht.", nameof(productId));
        return repository.RemoveAsync(userId, productId, cancellationToken);
    }
}
