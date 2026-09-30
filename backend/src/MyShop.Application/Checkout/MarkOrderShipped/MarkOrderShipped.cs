using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.MarkOrderShipped;

public sealed record MarkOrderShippedCommand(Guid Id, Guid Revision, string Carrier, string TrackingCode);
public enum MarkOrderShippedFailure { NotFound, InvalidTransition, ConcurrencyConflict }
public sealed record MarkOrderShippedResult(OrderStatusSnapshot? Order, MarkOrderShippedFailure? Failure)
{
    public static MarkOrderShippedResult Failed(MarkOrderShippedFailure failure) => new(null, failure);
}

public sealed class MarkOrderShipped(IOrderStatusRepository orders)
{
    private readonly IOrderStatusRepository orders = orders ?? throw new ArgumentNullException(nameof(orders));

    public async Task<MarkOrderShippedResult> ExecuteAsync(
        MarkOrderShippedCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Id == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(command));
        if (command.Revision == Guid.Empty) throw new ArgumentException("Revision is required.", nameof(command));
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Carrier, nameof(command));
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TrackingCode, nameof(command));
        var carrier = command.Carrier.Trim();
        var trackingCode = command.TrackingCode.Trim();
        if (carrier.Length > 100)
            throw new ArgumentException("Carrier must contain at most 100 characters.", nameof(command));
        if (trackingCode.Length > 100)
            throw new ArgumentException("Tracking code must contain at most 100 characters.", nameof(command));
        var order = await orders.GetStatusAsync(command.Id, cancellationToken);
        if (order is null) return MarkOrderShippedResult.Failed(MarkOrderShippedFailure.NotFound);
        if (order.Revision != command.Revision)
            return MarkOrderShippedResult.Failed(MarkOrderShippedFailure.ConcurrencyConflict);
        if (order.Status != OrderStatus.Paid)
            return MarkOrderShippedResult.Failed(MarkOrderShippedFailure.InvalidTransition);
        var shippedAt = DateTimeOffset.UtcNow;
        var revision = await orders.MarkShippedAsync(command.Id, command.Revision,
            shippedAt, carrier, trackingCode, cancellationToken);
        return revision is null
            ? MarkOrderShippedResult.Failed(MarkOrderShippedFailure.ConcurrencyConflict)
            : new(new(OrderStatus.Shipped, order.PaidAt, order.PaymentReference, shippedAt, carrier, trackingCode,
                null, null, null, null, null, revision.Value), null);
    }
}
