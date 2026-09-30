using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.MarkOrderPaid;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Tests.Checkout;

public sealed class MarkOrderPaidTests
{
    [Fact]
    public async Task MarksAwaitingOrderPaidWithItsCurrentRevision()
    {
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var repository = new Fake(Detail(id, revision, OrderStatus.AwaitingPayment));
        var before = DateTimeOffset.UtcNow;
        var result = await new MarkOrderPaid(repository)
            .ExecuteAsync(new(id, revision), CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Null(result.Failure);
        Assert.Equal(OrderStatus.Paid, result.Order!.Status);
        Assert.NotEqual(revision, result.Order.Revision);
        Assert.InRange(repository.PaidAt, before, after);
    }

    [Fact]
    public async Task RejectsStaleRevisionBeforeWriting()
    {
        var repository = new Fake(Detail(Guid.NewGuid(), Guid.NewGuid(), OrderStatus.AwaitingPayment));
        var result = await new MarkOrderPaid(repository)
            .ExecuteAsync(new(repository.Detail!.Id, Guid.NewGuid()), CancellationToken.None);
        Assert.Equal(MarkOrderPaidFailure.ConcurrencyConflict, result.Failure);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Fact]
    public async Task RejectsMissingAndAlreadyPaidOrdersBeforeWriting()
    {
        var missing = new Fake(null);
        var missingResult = await new MarkOrderPaid(missing)
            .ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        Assert.Equal(MarkOrderPaidFailure.NotFound, missingResult.Failure);

        var paid = new Fake(Detail(Guid.NewGuid(), Guid.NewGuid(), OrderStatus.Paid));
        var paidResult = await new MarkOrderPaid(paid)
            .ExecuteAsync(new(paid.Detail!.Id, paid.Detail.Revision), CancellationToken.None);
        Assert.Equal(MarkOrderPaidFailure.InvalidTransition, paidResult.Failure);
        Assert.Equal(0, paid.WriteCalls);
    }

    private static OrderDetail Detail(Guid id, Guid revision, OrderStatus status) => new(
        id, "MS-1", DateTimeOffset.UtcNow, "Ada", "ada@example.test", "Street 1", "1234 AB",
        "Utrecht", "NL", OrderPaymentMethod.PayLater, status,
        status == OrderStatus.Paid ? DateTimeOffset.UtcNow : null,
        null, null, null, null, null, revision, [], []);

    private sealed class Fake(OrderDetail? detail) : IOrderStatusRepository
    {
        public OrderDetail? Detail { get; } = detail;
        public int WriteCalls { get; private set; }
        public DateTimeOffset PaidAt { get; private set; }
        public Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Detail is null ? null : new OrderStatusSnapshot(
                Detail.Status, Detail.PaidAt, Detail.ShippedAt,
                Detail.ShippingCarrier, Detail.TrackingCode, Detail.CancelledAt,
                Detail.CancellationReason, Detail.Revision));
        public Task<Guid?> MarkPaidAsync(Guid id, Guid expectedRevision,
            DateTimeOffset paidAt, CancellationToken cancellationToken)
        {
            WriteCalls++; PaidAt = paidAt;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
        public Task<Guid?> MarkShippedAsync(Guid id, Guid expectedRevision,
            DateTimeOffset shippedAt, string carrier, string trackingCode,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> CancelAsync(Guid id, Guid expectedRevision, DateTimeOffset cancelledAt,
            string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
