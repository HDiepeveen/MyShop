using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.MarkOrderPaid;

public sealed record MarkOrderPaidCommand(Guid Id, Guid Revision);
public enum MarkOrderPaidFailure { NotFound, InvalidTransition, ConcurrencyConflict }
public sealed record MarkOrderPaidResult(OrderStatusSnapshot? Order, MarkOrderPaidFailure? Failure)
{
    public static MarkOrderPaidResult Failed(MarkOrderPaidFailure failure) => new(null, failure);
}

public sealed class MarkOrderPaid(IOrderStatusRepository orders)
{
    private readonly IOrderStatusRepository orders = orders ?? throw new ArgumentNullException(nameof(orders));

    public async Task<MarkOrderPaidResult> ExecuteAsync(
        MarkOrderPaidCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Id == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(command));
        if (command.Revision == Guid.Empty) throw new ArgumentException("Revision is required.", nameof(command));
        var order = await orders.GetStatusAsync(command.Id, cancellationToken);
        if (order is null) return MarkOrderPaidResult.Failed(MarkOrderPaidFailure.NotFound);
        if (order.Revision != command.Revision)
            return MarkOrderPaidResult.Failed(MarkOrderPaidFailure.ConcurrencyConflict);
        if (order.Status != OrderStatus.AwaitingPayment)
            return MarkOrderPaidResult.Failed(MarkOrderPaidFailure.InvalidTransition);
        var paidAt = DateTimeOffset.UtcNow;
        var revision = await orders.MarkPaidAsync(command.Id, command.Revision,
            paidAt, cancellationToken);
        return revision is null
            ? MarkOrderPaidResult.Failed(MarkOrderPaidFailure.ConcurrencyConflict)
            : new(new(OrderStatus.Paid, paidAt, null, revision.Value), null);
    }
}
