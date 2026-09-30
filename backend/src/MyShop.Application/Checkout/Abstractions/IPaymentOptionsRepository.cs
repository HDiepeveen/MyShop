namespace MyShop.Application.Checkout.Abstractions;

public interface IPaymentOptionsRepository
{
    Task<PaymentOptionsSnapshot> GetAsync(CancellationToken cancellationToken);
    Task<PaymentOptionsSnapshot?> SaveAsync(bool payLaterEnabled, bool onlinePaymentEnabled,
        Guid expectedRevision, CancellationToken cancellationToken);
}

public sealed record PaymentOptionsSnapshot(bool PayLaterEnabled, bool OnlinePaymentEnabled, Guid Revision);

public interface IOnlinePaymentAvailability
{
    bool IsConfigured { get; }
}
