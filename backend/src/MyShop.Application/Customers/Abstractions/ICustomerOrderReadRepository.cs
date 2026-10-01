using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Customers.Abstractions;

public interface ICustomerOrderReadRepository
{
    Task<CustomerOrderPage> ListAsync(string customerUserId, int offset, int limit,
        CancellationToken cancellationToken);
    Task<CustomerOrderDetail?> GetAsync(string customerUserId, Guid orderId,
        CancellationToken cancellationToken);
}

public sealed record CustomerOrderPage(IReadOnlyList<CustomerOrderListItem> Items, int TotalCount);

public sealed record CustomerOrderListItem(Guid Id, string Number, DateTimeOffset PlacedAt,
    OrderPaymentMethod PaymentMethod, OrderStatus Status, IReadOnlyList<OrderTotalSnapshot> Totals);

public sealed record CustomerOrderDetail(Guid Id, string Number, DateTimeOffset PlacedAt,
    string CustomerName, string Email, string AddressLine, string PostalCode, string City,
    string CountryCode, OrderPaymentMethod PaymentMethod, string? PaymentInstructions,
    OrderStatus Status, DateTimeOffset? PaidAt, DateTimeOffset? ShippedAt,
    string? ShippingCarrier, string? TrackingCode, DateTimeOffset? CancelledAt,
    DateTimeOffset? RefundedAt, IReadOnlyList<OrderLineSnapshot> Lines,
    IReadOnlyList<OrderTotalSnapshot> Totals);
