using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.ListOrders;

public sealed record ListOrdersQuery(
    int Offset = 0,
    int Limit = ListOrders.DefaultLimit,
    OrderStatus? Status = null);

public sealed class ListOrders
{
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 100;
    private readonly IOrderReadRepository _orders;

    public ListOrders(IOrderReadRepository orders) =>
        _orders = orders ?? throw new ArgumentNullException(nameof(orders));

    public async Task<OrderListPage> ExecuteAsync(
        ListOrdersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query.Offset), "Offset must not be negative.");
        if (query.Limit is < 1 or > MaximumLimit)
            throw new ArgumentOutOfRangeException(nameof(query.Limit), $"Limit must be between 1 and {MaximumLimit}.");
        if (query.Status is not null && !Enum.IsDefined(query.Status.Value))
            throw new ArgumentOutOfRangeException(nameof(query.Status), "Status is not supported.");
        return await _orders.ListAsync(query.Offset, query.Limit, query.Status, cancellationToken);
    }
}
