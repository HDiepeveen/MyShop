using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Customers.Abstractions;
using MyShop.Application.Customers.CancelCustomerOrder;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Tests.Customers;

public sealed class CancelCustomerOrderTests
{
    [Fact]
    public async Task CancelsOwnedAwaitingOrderWithFixedCustomerReason()
    {
        var scenario = new Scenario(OrderStatus.AwaitingPayment);
        var before = DateTimeOffset.UtcNow;
        var result = await scenario.Execute();
        var after = DateTimeOffset.UtcNow;

        Assert.Null(result.Failure);
        Assert.Equal(scenario.Replacement, result.Revision);
        Assert.InRange(result.CancelledAt!.Value, before, after);
        Assert.Equal("customer", scenario.WrittenCustomerUserId);
        Assert.Equal("Geannuleerd door klant.", scenario.Reason);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Refunded)]
    public async Task RejectsOrderThatNoLongerAwaitsPayment(OrderStatus status)
    {
        var scenario = new Scenario(status);
        var result = await scenario.Execute();
        Assert.Equal(CancelCustomerOrderFailure.InvalidTransition, result.Failure);
        Assert.Equal(0, scenario.WriteCalls);
    }

    [Fact]
    public async Task HidesMissingAndOtherCustomersOrdersAndDetectsConcurrency()
    {
        var missing = new Scenario(null);
        Assert.Equal(CancelCustomerOrderFailure.NotFound, (await missing.Execute()).Failure);
        var stale = new Scenario(OrderStatus.AwaitingPayment) { CommandRevision = Guid.NewGuid() };
        Assert.Equal(CancelCustomerOrderFailure.ConcurrencyConflict, (await stale.Execute()).Failure);
        Assert.Equal(0, stale.WriteCalls);
    }

    private sealed class Scenario : ICustomerOrderReadRepository, ICustomerOrderCancellationRepository
    {
        private readonly OrderStatus? status;
        public Guid OrderId { get; } = Guid.NewGuid();
        public Guid CurrentRevision { get; } = Guid.NewGuid();
        public Guid CommandRevision { get; init; }
        public Guid Replacement { get; } = Guid.NewGuid();
        public int WriteCalls { get; private set; }
        public string? WrittenCustomerUserId { get; private set; }
        public string? Reason { get; private set; }

        public Scenario(OrderStatus? status)
        {
            this.status = status;
            CommandRevision = CurrentRevision;
        }

        public Task<CancelCustomerOrderResult> Execute() => new CancelCustomerOrder(this, this)
            .ExecuteAsync(new("customer", OrderId, CommandRevision), CancellationToken.None);

        public Task<CustomerOrderDetail?> GetAsync(string customerUserId, Guid orderId,
            CancellationToken cancellationToken) => Task.FromResult(status is null ? null
                : new CustomerOrderDetail(OrderId, "MS-1", DateTimeOffset.UtcNow, "Ada",
                    "ada@example.test", "Straat 1", "1234 AB", "Utrecht", "NL",
                    OrderPaymentMethod.PayLater, null, status.Value, null, null, null, null,
                    null, null, CurrentRevision, [], []));

        public Task<CustomerOrderPage> ListAsync(string customerUserId, int offset, int limit,
            CancellationToken cancellationToken, OrderStatus? status = null, string? search = null) => throw new NotSupportedException();

        public Task<Guid?> CancelAsync(string customerUserId, Guid orderId, Guid expectedRevision,
            DateTimeOffset cancelledAt, string reason, CancellationToken cancellationToken)
        {
            WriteCalls++; WrittenCustomerUserId = customerUserId; Reason = reason;
            return Task.FromResult<Guid?>(Replacement);
        }
    }
}
