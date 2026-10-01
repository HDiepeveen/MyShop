namespace MyShop.Application.Checkout.Abstractions;

public sealed record OnlinePaymentStartRecord(Guid CheckoutToken, string ProviderName,
    string PaymentReference, string ProviderPaymentId, Uri CheckoutUrl,
    IReadOnlyList<OrderTotalSnapshot> Totals, OrderDeliveryMethodSnapshot DeliveryMethod,
    DateTimeOffset CreatedAt);

public interface IOnlinePaymentStartRepository
{
    Task<OnlinePaymentStartRecord?> GetByCheckoutTokenAsync(Guid checkoutToken,
        CancellationToken cancellationToken);
    Task SaveAsync(OnlinePaymentStartRecord payment, CancellationToken cancellationToken);
}
