namespace MyShop.Application.Checkout.Abstractions;

public interface IPaymentOptionsRepository
{
    Task<PaymentOptionsSnapshot> GetAsync(CancellationToken cancellationToken);
    Task<PaymentOptionsSnapshot?> SaveAsync(bool payLaterEnabled, bool onlinePaymentEnabled,
        string? payLaterInstructions, Guid expectedRevision, CancellationToken cancellationToken, bool checkoutEnabled = true);
}

public sealed record PaymentOptionsSnapshot(bool PayLaterEnabled, bool OnlinePaymentEnabled,
    string? PayLaterInstructions, Guid Revision)
{
    public bool CheckoutEnabled { get; init; } = true;
}

public interface IOnlinePaymentAvailability
{
    bool IsConfigured { get; }
    string? ProviderName { get; }
}
