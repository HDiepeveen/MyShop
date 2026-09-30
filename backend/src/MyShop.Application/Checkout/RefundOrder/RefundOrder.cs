using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.RefundOrder;

public sealed record RefundOrderCommand(Guid Id, Guid Revision, string RefundReference, string Reason);
public enum RefundOrderFailure { NotFound, InvalidTransition, ConcurrencyConflict }
public sealed record RefundOrderResult(OrderStatusSnapshot? Order, RefundOrderFailure? Failure)
{
    public static RefundOrderResult Failed(RefundOrderFailure failure) => new(null, failure);
}

public sealed class RefundOrder(IOrderStatusRepository orders)
{
    private readonly IOrderStatusRepository orders = orders ?? throw new ArgumentNullException(nameof(orders));

    public async Task<RefundOrderResult> ExecuteAsync(
        RefundOrderCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Id == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(command));
        if (command.Revision == Guid.Empty) throw new ArgumentException("Revision is required.", nameof(command));
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RefundReference, nameof(command));
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Reason, nameof(command));
        var refundReference = command.RefundReference.Trim();
        var reason = command.Reason.Trim();
        if (refundReference.Length > 100)
            throw new ArgumentException("Refund reference must contain at most 100 characters.", nameof(command));
        if (reason.Length > 500)
            throw new ArgumentException("Reason must contain at most 500 characters.", nameof(command));
        var order = await orders.GetStatusAsync(command.Id, cancellationToken);
        if (order is null) return RefundOrderResult.Failed(RefundOrderFailure.NotFound);
        if (order.Revision != command.Revision)
            return RefundOrderResult.Failed(RefundOrderFailure.ConcurrencyConflict);
        if (order.Status != OrderStatus.Paid)
            return RefundOrderResult.Failed(RefundOrderFailure.InvalidTransition);
        var refundedAt = DateTimeOffset.UtcNow;
        var revision = await orders.RefundAsync(command.Id, command.Revision, refundedAt,
            refundReference, reason, cancellationToken);
        return revision is null
            ? RefundOrderResult.Failed(RefundOrderFailure.ConcurrencyConflict)
            : new(new(OrderStatus.Refunded, order.PaidAt, order.PaymentReference,
                null, null, null, null, null, refundedAt, refundReference, reason,
                revision.Value), null);
    }
}
