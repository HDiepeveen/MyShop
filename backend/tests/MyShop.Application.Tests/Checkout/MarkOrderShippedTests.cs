using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.MarkOrderShipped;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Tests.Checkout;

public sealed class MarkOrderShippedTests
{
    [Fact]
    public async Task MarksPaidOrderShippedWithItsCurrentRevision()
    {
        var revision = Guid.NewGuid();
        var repository = new Fake(new(OrderStatus.Paid, DateTimeOffset.UtcNow, null, null, null, revision));
        var before = DateTimeOffset.UtcNow;
        var result = await new MarkOrderShipped(repository)
            .ExecuteAsync(new(Guid.NewGuid(), revision), CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Null(result.Failure);
        Assert.Equal(OrderStatus.Shipped, result.Order!.Status);
        Assert.NotEqual(revision, result.Order.Revision);
        Assert.InRange(repository.ShippedAt, before, after);
    }

    [Fact]
    public async Task RejectsStaleRevisionBeforeWriting()
    {
        var repository = new Fake(new(OrderStatus.Paid, DateTimeOffset.UtcNow, null, null, null, Guid.NewGuid()));
        var result = await new MarkOrderShipped(repository)
            .ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        Assert.Equal(MarkOrderShippedFailure.ConcurrencyConflict, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Theory]
    [InlineData(null, MarkOrderShippedFailure.NotFound)]
    [InlineData(OrderStatus.AwaitingPayment, MarkOrderShippedFailure.InvalidTransition)]
    [InlineData(OrderStatus.Shipped, MarkOrderShippedFailure.InvalidTransition)]
    public async Task RejectsMissingAndNonPaidOrders(OrderStatus? status, MarkOrderShippedFailure failure)
    {
        var revision = Guid.NewGuid();
        var repository = new Fake(status is null ? null : new(status.Value, null, null, null, null, revision));
        var result = await new MarkOrderShipped(repository)
            .ExecuteAsync(new(Guid.NewGuid(), revision), CancellationToken.None);
        Assert.Equal(failure, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    private sealed class Fake(OrderStatusSnapshot? current) : IOrderStatusRepository
    {
        public int WriteCalls { get; private set; }
        public DateTimeOffset ShippedAt { get; private set; }
        public Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(current);
        public Task<Guid?> MarkPaidAsync(Guid id, Guid expectedRevision,
            DateTimeOffset paidAt, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> MarkShippedAsync(Guid id, Guid expectedRevision,
            DateTimeOffset shippedAt, CancellationToken cancellationToken)
        {
            WriteCalls++; ShippedAt = shippedAt;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
        public Task<Guid?> CancelAsync(Guid id, Guid expectedRevision, DateTimeOffset cancelledAt,
            string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
