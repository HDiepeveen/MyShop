using MyShop.Application.Customers.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Customers.CancelCustomerOrder;

public sealed record CancelCustomerOrderCommand(string CustomerUserId, Guid OrderId, Guid Revision);
public enum CancelCustomerOrderFailure { NotFound, InvalidTransition, ConcurrencyConflict }
public sealed record CancelCustomerOrderResult(DateTimeOffset? CancelledAt, Guid? Revision,
    CancelCustomerOrderFailure? Failure)
{
    public static CancelCustomerOrderResult Failed(CancelCustomerOrderFailure failure) =>
        new(null, null, failure);
}

public sealed class CancelCustomerOrder(ICustomerOrderReadRepository orders,
    ICustomerOrderCancellationRepository cancellations)
{
    private const string CustomerCancellationReason = "Geannuleerd door klant.";
    private readonly ICustomerOrderReadRepository orders = orders
        ?? throw new ArgumentNullException(nameof(orders));
    private readonly ICustomerOrderCancellationRepository cancellations = cancellations
        ?? throw new ArgumentNullException(nameof(cancellations));

    public async Task<CancelCustomerOrderResult> ExecuteAsync(CancelCustomerOrderCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CustomerUserId);
        if (command.OrderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(command));
        if (command.Revision == Guid.Empty)
            throw new ArgumentException("Revision is required.", nameof(command));
        var order = await orders.GetAsync(command.CustomerUserId, command.OrderId, cancellationToken);
        if (order is null) return CancelCustomerOrderResult.Failed(CancelCustomerOrderFailure.NotFound);
        if (order.Revision != command.Revision)
            return CancelCustomerOrderResult.Failed(CancelCustomerOrderFailure.ConcurrencyConflict);
        if (order.Status != OrderStatus.AwaitingPayment)
            return CancelCustomerOrderResult.Failed(CancelCustomerOrderFailure.InvalidTransition);
        var cancelledAt = DateTimeOffset.UtcNow;
        var revision = await cancellations.CancelAsync(command.CustomerUserId, command.OrderId,
            command.Revision, cancelledAt, CustomerCancellationReason, cancellationToken);
        return revision is null
            ? CancelCustomerOrderResult.Failed(CancelCustomerOrderFailure.ConcurrencyConflict)
            : new(cancelledAt, revision, null);
    }
}
