using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Application.Checkout.GetOrder;

public sealed class GetOrder
{
    private readonly IOrderReadRepository _orders;

    public GetOrder(IOrderReadRepository orders) =>
        _orders = orders ?? throw new ArgumentNullException(nameof(orders));

    public async Task<OrderDetail?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (id == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(id));
        return await _orders.GetAsync(id, cancellationToken);
    }
}
