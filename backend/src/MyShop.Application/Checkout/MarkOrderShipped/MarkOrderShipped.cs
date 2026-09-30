using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.MarkOrderShipped;

public sealed record MarkOrderShippedCommand(Guid Id, Guid Revision);
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
        var order = await orders.GetStatusAsync(command.Id, cancellationToken);
        if (order is null) return MarkOrderShippedResult.Failed(MarkOrderShippedFailure.NotFound);
        if (order.Revision != command.Revision)
            return MarkOrderShippedResult.Failed(MarkOrderShippedFailure.ConcurrencyConflict);
        if (order.Status != OrderStatus.Paid)
            return MarkOrderShippedResult.Failed(MarkOrderShippedFailure.InvalidTransition);
        var shippedAt = DateTimeOffset.UtcNow;
        var revision = await orders.MarkShippedAsync(command.Id, command.Revision,
            shippedAt, cancellationToken);
        return revision is null
            ? MarkOrderShippedResult.Failed(MarkOrderShippedFailure.ConcurrencyConflict)
            : new(new(OrderStatus.Shipped, order.PaidAt, shippedAt, revision.Value), null);
    }
}
