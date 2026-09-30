using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.CancelOrder;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Tests.Checkout;

public sealed class CancelOrderTests
{
    [Fact]
    public async Task CancelsAwaitingOrderWithTrimmedReasonAndCurrentRevision()
    {
        var revision = Guid.NewGuid();
        var repository = new Fake(new(
            OrderStatus.AwaitingPayment, null, null, null, null, null, null, revision));
        var before = DateTimeOffset.UtcNow;
        var result = await new CancelOrder(repository)
            .ExecuteAsync(new(Guid.NewGuid(), revision, "  Klant ziet af van bestelling.  "), CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Null(result.Failure);
        Assert.Equal(OrderStatus.Cancelled, result.Order!.Status);
        Assert.Equal("Klant ziet af van bestelling.", result.Order.CancellationReason);
        Assert.Equal(result.Order.CancellationReason, repository.Reason);
        Assert.InRange(repository.CancelledAt, before, after);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RejectsEmptyReasonBeforeReading(string reason)
    {
        var repository = new Fake(null);
        await Assert.ThrowsAsync<ArgumentException>(() => new CancelOrder(repository)
            .ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid(), reason), CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task RejectsReasonLongerThanMaximumBeforeReading()
    {
        var repository = new Fake(null);
        await Assert.ThrowsAsync<ArgumentException>(() => new CancelOrder(repository)
            .ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid(), new string('x', 501)), CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task RejectsStaleRevisionBeforeWriting()
    {
        var repository = new Fake(new(
            OrderStatus.AwaitingPayment, null, null, null, null, null, null, Guid.NewGuid()));
        var result = await new CancelOrder(repository)
            .ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid(), "Reden"), CancellationToken.None);

        Assert.Equal(CancelOrderFailure.ConcurrencyConflict, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Theory]
    [InlineData(null, CancelOrderFailure.NotFound)]
    [InlineData(OrderStatus.Paid, CancelOrderFailure.InvalidTransition)]
    [InlineData(OrderStatus.Shipped, CancelOrderFailure.InvalidTransition)]
    [InlineData(OrderStatus.Cancelled, CancelOrderFailure.InvalidTransition)]
    public async Task RejectsMissingAndNonAwaitingOrders(OrderStatus? status, CancelOrderFailure failure)
    {
        var revision = Guid.NewGuid();
        var repository = new Fake(status is null ? null : new(
            status.Value, null, null, null, null, null, null, revision));
        var result = await new CancelOrder(repository)
            .ExecuteAsync(new(Guid.NewGuid(), revision, "Reden"), CancellationToken.None);
        Assert.Equal(failure, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    private sealed class Fake(OrderStatusSnapshot? current) : IOrderStatusRepository
    {
        public int ReadCalls { get; private set; }
        public int WriteCalls { get; private set; }
        public DateTimeOffset CancelledAt { get; private set; }
        public string? Reason { get; private set; }
        public Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken)
        {
            ReadCalls++; return Task.FromResult(current);
        }
        public Task<Guid?> MarkPaidAsync(Guid id, Guid expectedRevision,
            DateTimeOffset paidAt, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> MarkShippedAsync(Guid id, Guid expectedRevision,
            DateTimeOffset shippedAt, string carrier, string trackingCode,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> CancelAsync(Guid id, Guid expectedRevision, DateTimeOffset cancelledAt,
            string reason, CancellationToken cancellationToken)
        {
            WriteCalls++; CancelledAt = cancelledAt; Reason = reason;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
    }
}
