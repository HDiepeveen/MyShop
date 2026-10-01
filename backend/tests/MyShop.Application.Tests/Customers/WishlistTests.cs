using MyShop.Application.Customers.Abstractions;
using MyShop.Application.Customers.ManageWishlist;

namespace MyShop.Application.Tests.Customers;

public sealed class WishlistTests
{
    [Fact]
    public async Task List_validates_paging_and_forwards_customer_scope()
    {
        var repository = new Repository();
        var useCase = new ListWishlist(repository);
        await useCase.ExecuteAsync(new("customer", 4, 10), CancellationToken.None);
        Assert.Equal(("customer", 4, 10), repository.ListRequest);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            useCase.ExecuteAsync(new("customer", -1, 10), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            useCase.ExecuteAsync(new("customer", 0, 101), CancellationToken.None));
    }

    [Fact]
    public async Task Add_uses_utc_time_and_reports_product_availability()
    {
        var repository = new Repository { AddResult = true };
        var now = new DateTimeOffset(2026, 10, 1, 10, 30, 0, TimeSpan.Zero);
        var result = await new AddWishlistItem(repository, new FixedTimeProvider(now))
            .ExecuteAsync("customer", Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);
        Assert.True(result);
        Assert.Equal(now, repository.AddedAt);
    }

    [Fact]
    public async Task State_and_remove_are_scoped_to_customer_and_product()
    {
        var repository = new Repository { ContainsResult = true };
        var productId = Guid.NewGuid();
        Assert.True(await new GetWishlistState(repository).ExecuteAsync("customer", productId, CancellationToken.None));
        await new RemoveWishlistItem(repository).ExecuteAsync("customer", productId, CancellationToken.None);
        Assert.Equal(("customer", productId), repository.Removed);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Repository : IWishlistRepository
    {
        public (string, int, int)? ListRequest { get; private set; }
        public bool AddResult { get; init; }
        public bool ContainsResult { get; init; }
        public DateTimeOffset? AddedAt { get; private set; }
        public (string, Guid)? Removed { get; private set; }
        public Task<WishlistPage> ListAsync(string userId, int offset, int limit, CancellationToken cancellationToken)
        { ListRequest = (userId, offset, limit); return Task.FromResult(new WishlistPage([], 0)); }
        public Task<bool> ContainsAsync(string userId, Guid productId, CancellationToken cancellationToken) => Task.FromResult(ContainsResult);
        public Task<bool> AddAsync(string userId, Guid productId, DateTimeOffset addedAt, CancellationToken cancellationToken)
        { AddedAt = addedAt; return Task.FromResult(AddResult); }
        public Task RemoveAsync(string userId, Guid productId, CancellationToken cancellationToken)
        { Removed = (userId, productId); return Task.CompletedTask; }
    }
}
