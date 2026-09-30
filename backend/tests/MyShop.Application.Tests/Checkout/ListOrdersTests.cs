using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.ListOrders;
using MyShop.Domain.Checkout;
using UseCase = MyShop.Application.Checkout.ListOrders.ListOrders;

namespace MyShop.Application.Tests.Checkout;

public sealed class ListOrdersTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsValidPagingAndCancellation()
    {
        var expected = new OrderListPage([], 12);
        var repository = new Fake(expected);
        var useCase = new UseCase(repository);
        using var source = new CancellationTokenSource();
        var result = await useCase.ExecuteAsync(
            new ListOrdersQuery(5, 10, OrderStatus.Paid), source.Token);
        Assert.Same(expected, result);
        Assert.Equal((5, 10, OrderStatus.Paid, source.Token),
            (repository.Offset, repository.Limit, repository.Status, repository.Token));
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public async Task ExecuteAsync_RejectsInvalidPagingBeforeRepositoryAccess(int offset, int limit)
    {
        var repository = new Fake(new([], 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new UseCase(repository).ExecuteAsync(new(offset, limit), CancellationToken.None));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepository()
    {
        var repository = new Fake(new([], 0));
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UseCase(repository).ExecuteAsync(new(), source.Token));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsUnsupportedStatusBeforeRepositoryAccess()
    {
        var repository = new Fake(new([], 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new UseCase(repository).ExecuteAsync(new(Status: (OrderStatus)999), CancellationToken.None));
        Assert.Equal(0, repository.Calls);
    }

    private sealed class Fake(OrderListPage page) : IOrderReadRepository
    {
        public int Calls { get; private set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public OrderStatus? Status { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<OrderListPage> ListAsync(int offset, int limit, OrderStatus? status,
            CancellationToken cancellationToken)
        {
            Calls++; Offset = offset; Limit = limit; Status = status; Token = cancellationToken;
            return Task.FromResult(page);
        }
        public Task<OrderDetail?> GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
