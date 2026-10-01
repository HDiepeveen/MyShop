using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Customers.Abstractions;
using MyShop.Application.Customers.GetCustomerOrder;
using MyShop.Application.Customers.ListCustomerOrders;

namespace MyShop.Application.Tests.Customers;

public sealed class CustomerOrderTests
{
    [Fact]
    public async Task ListValidatesPagingAndForwardsCustomerIdentity()
    {
        var repository = new Repository();
        var result = await new ListCustomerOrders(repository).ExecuteAsync(
            new("customer", 20, 10), CancellationToken.None);
        Assert.Same(repository.Page, result);
        Assert.Equal(("customer", 20, 10),
            (repository.CustomerUserId, repository.Offset, repository.Limit));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new ListCustomerOrders(repository).ExecuteAsync(new("customer", -1, 20), default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new ListCustomerOrders(repository).ExecuteAsync(new("customer", 0, 101), default));
    }

    [Fact]
    public async Task DetailForwardsBothOwnerAndOrderIdentity()
    {
        var repository = new Repository();
        var orderId = Guid.NewGuid();
        await new GetCustomerOrder(repository).ExecuteAsync("customer", orderId, default);
        Assert.Equal(("customer", orderId), (repository.CustomerUserId, repository.OrderId));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetCustomerOrder(repository).ExecuteAsync("customer", Guid.Empty, default));
    }

    private sealed class Repository : ICustomerOrderReadRepository
    {
        public CustomerOrderPage Page { get; } = new([], 0);
        public string? CustomerUserId { get; private set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public Guid OrderId { get; private set; }
        public Task<CustomerOrderPage> ListAsync(string customerUserId, int offset, int limit,
            CancellationToken cancellationToken)
        {
            CustomerUserId = customerUserId; Offset = offset; Limit = limit;
            return Task.FromResult(Page);
        }
        public Task<CustomerOrderDetail?> GetAsync(string customerUserId, Guid orderId,
            CancellationToken cancellationToken)
        {
            CustomerUserId = customerUserId; OrderId = orderId;
            return Task.FromResult<CustomerOrderDetail?>(null);
        }
    }
}
