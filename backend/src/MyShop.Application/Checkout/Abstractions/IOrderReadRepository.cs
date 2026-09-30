using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.Abstractions;

public interface IOrderReadRepository
{
    Task<OrderListPage> ListAsync(int offset, int limit, OrderStatus? status, string? search,
        CancellationToken cancellationToken);
    Task<OrderDetail?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record OrderListItem(
    Guid Id,
    string Number,
    DateTimeOffset PlacedAt,
    string CustomerName,
    OrderPaymentMethod PaymentMethod,
    OrderStatus Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    DateTimeOffset? RefundedAt,
    string? RefundReason,
    IReadOnlyList<OrderTotalSnapshot> Totals);

public sealed record OrderListPage(IReadOnlyList<OrderListItem> Items, int TotalCount);

public sealed record OrderDetail(
    Guid Id,
    string Number,
    DateTimeOffset PlacedAt,
    string CustomerName,
    string Email,
    string AddressLine,
    string PostalCode,
    string City,
    string CountryCode,
    OrderPaymentMethod PaymentMethod,
    OrderStatus Status,
    DateTimeOffset? PaidAt,
    string? PaymentReference,
    DateTimeOffset? ShippedAt,
    string? ShippingCarrier,
    string? TrackingCode,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    DateTimeOffset? RefundedAt,
    string? RefundReference,
    string? RefundReason,
    Guid Revision,
    IReadOnlyList<OrderLineSnapshot> Lines,
    IReadOnlyList<OrderTotalSnapshot> Totals);

public sealed record OrderLineSnapshot(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    int Quantity,
    decimal UnitAmount,
    string Currency,
    decimal TotalAmount);

public sealed record OrderTotalSnapshot(string Currency, decimal Amount);

public interface IOrderStatusRepository
{
    Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid?> MarkPaidAsync(Guid id, Guid expectedRevision,
        DateTimeOffset paidAt, string paymentReference, CancellationToken cancellationToken);
    Task<Guid?> MarkShippedAsync(Guid id, Guid expectedRevision,
        DateTimeOffset shippedAt, string carrier, string trackingCode,
        CancellationToken cancellationToken);
    Task<Guid?> CancelAsync(Guid id, Guid expectedRevision, DateTimeOffset cancelledAt,
        string reason, CancellationToken cancellationToken);
    Task<Guid?> RefundAsync(Guid id, Guid expectedRevision, DateTimeOffset refundedAt,
        string refundReference, string reason, CancellationToken cancellationToken);
}

public sealed record OrderStatusSnapshot(
    OrderStatus Status,
    DateTimeOffset? PaidAt,
    string? PaymentReference,
    DateTimeOffset? ShippedAt,
    string? ShippingCarrier,
    string? TrackingCode,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    DateTimeOffset? RefundedAt,
    string? RefundReference,
    string? RefundReason,
    Guid Revision);
