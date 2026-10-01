namespace MyShop.Application.Checkout.Abstractions;

public sealed record OnlinePaymentProviderRequest(string ProviderName, Guid CheckoutToken,
    string PaymentReference, IReadOnlyList<OrderTotalSnapshot> Totals);

public sealed record OnlinePaymentProviderStart(string ProviderPaymentId, Uri CheckoutUrl);

public interface IOnlinePaymentProvider
{
    Task<OnlinePaymentProviderStart> StartAsync(OnlinePaymentProviderRequest request,
        CancellationToken cancellationToken);
}