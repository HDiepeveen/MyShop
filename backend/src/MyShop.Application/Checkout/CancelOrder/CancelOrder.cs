using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.CancelOrder;

public sealed record CancelOrderCommand(Guid Id, Guid Revision, string Reason);
public enum CancelOrderFailure { NotFound, InvalidTransition, ConcurrencyConflict }
public sealed record CancelOrderResult(OrderStatusSnapshot? Order, CancelOrderFailure? Failure)
{
    public static CancelOrderResult Failed(CancelOrderFailure failure) => new(null, failure);
}

public sealed class CancelOrder(IOrderStatusRepository orders)
{
    private readonly IOrderStatusRepository orders = orders ?? throw new ArgumentNullException(nameof(orders));

    public async Task<CancelOrderResult> ExecuteAsync(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Id == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(command));
        if (command.Revision == Guid.Empty) throw new ArgumentException("Revision is required.", nameof(command));
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Reason, nameof(command));
        var reason = command.Reason.Trim();
        if (reason.Length > 500) throw new ArgumentException("Reason must contain at most 500 characters.", nameof(command));
        var order = await orders.GetStatusAsync(command.Id, cancellationToken);
        if (order is null) return CancelOrderResult.Failed(CancelOrderFailure.NotFound);
        if (order.Revision != command.Revision)
            return CancelOrderResult.Failed(CancelOrderFailure.ConcurrencyConflict);
        if (order.Status != OrderStatus.AwaitingPayment)
            return CancelOrderResult.Failed(CancelOrderFailure.InvalidTransition);
        var cancelledAt = DateTimeOffset.UtcNow;
        var revision = await orders.CancelAsync(command.Id, command.Revision,
            cancelledAt, reason, cancellationToken);
        return revision is null
            ? CancelOrderResult.Failed(CancelOrderFailure.ConcurrencyConflict)
            : new(new(OrderStatus.Cancelled, null, null, null, null, null,
                cancelledAt, reason, revision.Value), null);
    }
}
