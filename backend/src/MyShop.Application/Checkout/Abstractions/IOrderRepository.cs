using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.Abstractions;

public sealed record OrderReceipt(Guid Id, string Number, DateTimeOffset PlacedAt,
    string? PaymentInstructions);

public interface IOrderRepository
{
    Task<OrderReceipt?> GetByCheckoutTokenAsync(Guid checkoutToken, CancellationToken cancellationToken);
    Task<OrderReceipt> AddAsync(Order order, Guid checkoutToken, CancellationToken cancellationToken);
}
