namespace MyShop.Application.Checkout.Abstractions;

public sealed record OnlinePaymentStartCustomerSnapshot(string Name, string Email);
public sealed record OnlinePaymentStartAddressSnapshot(string AddressLine, string PostalCode,
    string City, string CountryCode);
public sealed record OnlinePaymentStartLineSnapshot(Guid ProductId, Guid VariantId,
    string ProductName, string VariantName, int Quantity, decimal UnitAmount, string Currency,
    decimal TotalAmount, decimal? VatRate = null, bool VatExempt = false);

public sealed record OnlinePaymentStartRecord(Guid CheckoutToken, string ProviderName,
    string PaymentReference, string ProviderPaymentId, Uri CheckoutUrl,
    OnlinePaymentStartCustomerSnapshot Customer, OnlinePaymentStartAddressSnapshot Address,
    IReadOnlyList<OnlinePaymentStartLineSnapshot> Lines,
    IReadOnlyList<OrderTotalSnapshot> Totals, OrderDeliveryMethodSnapshot DeliveryMethod,
    DateTimeOffset CreatedAt, string? CustomerUserId = null);

public interface IOnlinePaymentStartRepository
{
    Task<OnlinePaymentStartRecord?> GetByCheckoutTokenAsync(Guid checkoutToken,
        CancellationToken cancellationToken);
    Task SaveAsync(OnlinePaymentStartRecord payment, CancellationToken cancellationToken);
}
