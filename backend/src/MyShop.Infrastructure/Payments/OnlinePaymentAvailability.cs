using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Infrastructure.Payments;

// Replaced by the selected provider adapter in the online-payment slice.
internal sealed class OnlinePaymentAvailability : IOnlinePaymentAvailability
{
    public bool IsConfigured => false;
}
