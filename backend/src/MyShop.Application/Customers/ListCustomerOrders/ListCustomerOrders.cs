using MyShop.Application.Customers.Abstractions;

namespace MyShop.Application.Customers.ListCustomerOrders;

public sealed record ListCustomerOrdersQuery(string CustomerUserId, int Offset = 0,
    int Limit = ListCustomerOrders.DefaultLimit);

public sealed class ListCustomerOrders(ICustomerOrderReadRepository orders)
{
    public const int DefaultLimit = 20;
    public const int MaximumLimit = 100;
    private readonly ICustomerOrderReadRepository orders = orders
        ?? throw new ArgumentNullException(nameof(orders));

    public Task<CustomerOrderPage> ExecuteAsync(ListCustomerOrdersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(query.CustomerUserId);
        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query.Offset));
        if (query.Limit is < 1 or > MaximumLimit)
            throw new ArgumentOutOfRangeException(nameof(query.Limit));
        return orders.ListAsync(query.CustomerUserId, query.Offset, query.Limit, cancellationToken);
    }
}
