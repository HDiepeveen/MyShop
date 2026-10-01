using Microsoft.Extensions.Configuration;
using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Infrastructure.Payments;

internal sealed class OnlinePaymentAvailability : IOnlinePaymentAvailability
{
    public const string ProviderKey = "Payments:Online:Provider";

    public OnlinePaymentAvailability(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ProviderName = Normalize(configuration[ProviderKey]);
    }

    public bool IsConfigured => ProviderName is not null;
    public string? ProviderName { get; }

    private static string? Normalize(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
