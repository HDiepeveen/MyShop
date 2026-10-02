using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.Abstractions;

public sealed record OrderReceipt(Guid Id, string Number, DateTimeOffset PlacedAt,
    string? PaymentInstructions, IReadOnlyList<OrderTotalSnapshot> Totals,
    OrderDeliveryMethodSnapshot? DeliveryMethod);
public sealed record StockReservation(Guid ProductId, Guid VariantId, int Quantity);

public interface IOrderRepository
{
    Task<OrderReceipt?> GetByCheckoutTokenAsync(Guid checkoutToken, CancellationToken cancellationToken);
    Task<OrderReceipt?> AddAsync(Order order, Guid checkoutToken, string? customerUserId,
        IReadOnlyList<StockReservation> stock, CancellationToken cancellationToken);
    Task<OrderReceipt?> AddPaidAsync(Order order, Guid checkoutToken, string paymentReference,
        IReadOnlyList<StockReservation> stock, CancellationToken cancellationToken);
}
