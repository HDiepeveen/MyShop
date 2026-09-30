using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.GetOrder;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Tests.Checkout;

public sealed class GetOrderTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsIdAndToken()
    {
        var id = Guid.NewGuid();
        var repository = new Fake();
        using var source = new CancellationTokenSource();
        await new GetOrder(repository).ExecuteAsync(id, source.Token);
        Assert.Equal(id, repository.Id);
        Assert.Equal(source.Token, repository.Token);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsEmptyIdBeforeRepositoryAccess()
    {
        var repository = new Fake();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetOrder(repository).ExecuteAsync(Guid.Empty, CancellationToken.None));
        Assert.Equal(0, repository.Calls);
    }

    private sealed class Fake : IOrderReadRepository
    {
        public int Calls { get; private set; }
        public Guid Id { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<OrderDetail?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            Calls++; Id = id; Token = cancellationToken; return Task.FromResult<OrderDetail?>(null);
        }
        public Task<OrderListPage> ListAsync(int offset, int limit, OrderStatus? status,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
