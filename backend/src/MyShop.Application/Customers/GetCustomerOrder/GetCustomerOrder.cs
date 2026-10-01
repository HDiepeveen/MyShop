using MyShop.Application.Customers.Abstractions;

namespace MyShop.Application.Customers.GetCustomerOrder;

public sealed class GetCustomerOrder(ICustomerOrderReadRepository orders)
{
    private readonly ICustomerOrderReadRepository orders = orders
        ?? throw new ArgumentNullException(nameof(orders));

    public Task<CustomerOrderDetail?> ExecuteAsync(string customerUserId, Guid orderId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(customerUserId);
        if (orderId == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(orderId));
        return orders.GetAsync(customerUserId, orderId, cancellationToken);
    }
}
