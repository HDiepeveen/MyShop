using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.RefundOrder;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Tests.Checkout;

public sealed class RefundOrderTests
{
    [Fact]
    public async Task RefundsPaidOrderWithTrimmedDetailsAndCurrentRevision()
    {
        var revision = Guid.NewGuid();
        var paidAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var repository = new Fake(Snapshot(OrderStatus.Paid, revision, paidAt));
        var before = DateTimeOffset.UtcNow;
        var result = await new RefundOrder(repository).ExecuteAsync(
            new(Guid.NewGuid(), revision, "  bankafschrift 67890  ", "  Dubbele betaling.  "),
            CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Null(result.Failure);
        Assert.Equal(OrderStatus.Refunded, result.Order!.Status);
        Assert.Equal(paidAt, result.Order.PaidAt);
        Assert.Equal("bankafschrift 12345", result.Order.PaymentReference);
        Assert.Equal("bankafschrift 67890", result.Order.RefundReference);
        Assert.Equal("Dubbele betaling.", result.Order.RefundReason);
        Assert.Equal(result.Order.RefundReference, repository.RefundReference);
        Assert.Equal(result.Order.RefundReason, repository.Reason);
        Assert.InRange(repository.RefundedAt, before, after);
        Assert.NotEqual(revision, result.Order.Revision);
    }

    [Theory]
    [InlineData("", "Reden")]
    [InlineData(" ", "Reden")]
    [InlineData("kenmerk", "")]
    [InlineData("kenmerk", " ")]
    public async Task RejectsMissingDetailsBeforeReading(string refundReference, string reason)
    {
        var repository = new Fake(null);
        await Assert.ThrowsAsync<ArgumentException>(() => new RefundOrder(repository).ExecuteAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), refundReference, reason), CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectsDetailsLongerThanMaximumBeforeReading(bool referenceTooLong)
    {
        var repository = new Fake(null);
        await Assert.ThrowsAsync<ArgumentException>(() => new RefundOrder(repository).ExecuteAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), referenceTooLong ? new string('x', 101) : "kenmerk",
                referenceTooLong ? "Reden" : new string('x', 501)), CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task RejectsStaleRevisionBeforeWriting()
    {
        var repository = new Fake(Snapshot(OrderStatus.Paid, Guid.NewGuid()));
        var result = await new RefundOrder(repository).ExecuteAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), "kenmerk", "Reden"), CancellationToken.None);
        Assert.Equal(RefundOrderFailure.ConcurrencyConflict, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Theory]
    [InlineData(null, RefundOrderFailure.NotFound)]
    [InlineData(OrderStatus.AwaitingPayment, RefundOrderFailure.InvalidTransition)]
    [InlineData(OrderStatus.Shipped, RefundOrderFailure.InvalidTransition)]
    [InlineData(OrderStatus.Cancelled, RefundOrderFailure.InvalidTransition)]
    [InlineData(OrderStatus.Refunded, RefundOrderFailure.InvalidTransition)]
    public async Task RejectsMissingAndNonPaidOrders(OrderStatus? status, RefundOrderFailure failure)
    {
        var revision = Guid.NewGuid();
        var repository = new Fake(status is null ? null : Snapshot(status.Value, revision));
        var result = await new RefundOrder(repository).ExecuteAsync(
            new(Guid.NewGuid(), revision, "kenmerk", "Reden"), CancellationToken.None);
        Assert.Equal(failure, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    private static OrderStatusSnapshot Snapshot(OrderStatus status, Guid revision,
        DateTimeOffset? paidAt = null) => new(status, paidAt, paidAt is null ? null : "bankafschrift 12345",
            null, null, null, null, null, null, null, null, revision);

    private sealed class Fake(OrderStatusSnapshot? current) : IOrderStatusRepository
    {
        public int ReadCalls { get; private set; }
        public int WriteCalls { get; private set; }
        public DateTimeOffset RefundedAt { get; private set; }
        public string? RefundReference { get; private set; }
        public string? Reason { get; private set; }
        public Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken)
        {
            ReadCalls++;
            return Task.FromResult(current);
        }
        public Task<Guid?> MarkPaidAsync(Guid id, Guid expectedRevision, DateTimeOffset paidAt,
            string paymentReference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> MarkShippedAsync(Guid id, Guid expectedRevision, DateTimeOffset shippedAt,
            string carrier, string trackingCode, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> CancelAsync(Guid id, Guid expectedRevision, DateTimeOffset cancelledAt,
            string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> RefundAsync(Guid id, Guid expectedRevision, DateTimeOffset refundedAt,
            string refundReference, string reason, CancellationToken cancellationToken)
        {
            WriteCalls++;
            RefundedAt = refundedAt;
            RefundReference = refundReference;
            Reason = reason;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
    }
}
