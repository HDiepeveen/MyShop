namespace MyShop.Application.Checkout.Abstractions;

public interface IPaymentOptionsRepository
{
    Task<PaymentOptionsSnapshot> GetAsync(CancellationToken cancellationToken);
    Task<PaymentOptionsSnapshot?> SaveAsync(bool payLaterEnabled, bool onlinePaymentEnabled,
        string? payLaterInstructions, Guid expectedRevision, CancellationToken cancellationToken);
}

public sealed record PaymentOptionsSnapshot(bool PayLaterEnabled, bool OnlinePaymentEnabled,
    string? PayLaterInstructions, Guid Revision);

public interface IOnlinePaymentAvailability
{
    bool IsConfigured { get; }
    string? ProviderName { get; }
}
