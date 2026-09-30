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
        var repository = new Fake(new(
            OrderStatus.Paid, DateTimeOffset.UtcNow, "bank-reference", null, null, null, null, null,
            null, null, null, revision));
        var before = DateTimeOffset.UtcNow;
        var result = await new MarkOrderShipped(repository)
            .ExecuteAsync(new(Guid.NewGuid(), revision, "  PostNL  ", "  3SMYSHOP123  "),
                CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Null(result.Failure);
        Assert.Equal(OrderStatus.Shipped, result.Order!.Status);
        Assert.NotEqual(revision, result.Order.Revision);
        Assert.Equal("PostNL", result.Order.ShippingCarrier);
        Assert.Equal("3SMYSHOP123", result.Order.TrackingCode);
        Assert.Equal("bank-reference", result.Order.PaymentReference);
        Assert.Equal(result.Order.ShippingCarrier, repository.Carrier);
        Assert.Equal(result.Order.TrackingCode, repository.TrackingCode);
        Assert.InRange(repository.ShippedAt, before, after);
    }

    [Theory]
    [InlineData("", "code")]
    [InlineData(" ", "code")]
    [InlineData("PostNL", "")]
    [InlineData("PostNL", " ")]
    public async Task RejectsMissingShipmentDetailsBeforeReading(string carrier, string trackingCode)
    {
        var repository = new Fake(null);
        await Assert.ThrowsAsync<ArgumentException>(() => new MarkOrderShipped(repository)
            .ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid(), carrier, trackingCode),
                CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectsShipmentDetailsLongerThanMaximumBeforeReading(bool carrierTooLong)
    {
        var repository = new Fake(null);
        await Assert.ThrowsAsync<ArgumentException>(() => new MarkOrderShipped(repository)
            .ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid(),
                    carrierTooLong ? new string('x', 101) : "PostNL",
                    carrierTooLong ? "code" : new string('x', 101)),
                CancellationToken.None));
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task RejectsStaleRevisionBeforeWriting()
    {
        var repository = new Fake(new(OrderStatus.Paid, DateTimeOffset.UtcNow,
            "bank-reference", null, null, null, null, null, null, null, null, Guid.NewGuid()));
        var result = await new MarkOrderShipped(repository)
            .ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid(), "PostNL", "code"), CancellationToken.None);
        Assert.Equal(MarkOrderShippedFailure.ConcurrencyConflict, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Theory]
    [InlineData(null, MarkOrderShippedFailure.NotFound)]
    [InlineData(OrderStatus.AwaitingPayment, MarkOrderShippedFailure.InvalidTransition)]
    [InlineData(OrderStatus.Shipped, MarkOrderShippedFailure.InvalidTransition)]
    [InlineData(OrderStatus.Refunded, MarkOrderShippedFailure.InvalidTransition)]
    public async Task RejectsMissingAndNonPaidOrders(OrderStatus? status, MarkOrderShippedFailure failure)
    {
        var revision = Guid.NewGuid();
        var repository = new Fake(status is null ? null : new(
            status.Value, null, null, null, null, null, null, null,
            null, null, null, revision));
        var result = await new MarkOrderShipped(repository)
            .ExecuteAsync(new(Guid.NewGuid(), revision, "PostNL", "code"), CancellationToken.None);
        Assert.Equal(failure, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    private sealed class Fake(OrderStatusSnapshot? current) : IOrderStatusRepository
    {
        public int WriteCalls { get; private set; }
        public int ReadCalls { get; private set; }
        public DateTimeOffset ShippedAt { get; private set; }
        public string? Carrier { get; private set; }
        public string? TrackingCode { get; private set; }
        public Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken)
        {
            ReadCalls++; return Task.FromResult(current);
        }
        public Task<Guid?> MarkPaidAsync(Guid id, Guid expectedRevision,
            DateTimeOffset paidAt, string paymentReference,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> MarkShippedAsync(Guid id, Guid expectedRevision,
            DateTimeOffset shippedAt, string carrier, string trackingCode,
            CancellationToken cancellationToken)
        {
            WriteCalls++; ShippedAt = shippedAt; Carrier = carrier; TrackingCode = trackingCode;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
        public Task<Guid?> CancelAsync(Guid id, Guid expectedRevision, DateTimeOffset cancelledAt,
            string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> RefundAsync(Guid id, Guid expectedRevision, DateTimeOffset refundedAt,
            string refundReference, string reason,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
