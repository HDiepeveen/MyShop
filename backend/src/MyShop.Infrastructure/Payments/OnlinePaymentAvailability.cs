using Microsoft.Extensions.Configuration;
using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Infrastructure.Payments;

internal sealed class OnlinePaymentAvailability : IOnlinePaymentAvailability
{
    public const string ProviderKey = "Payments:Online:Provider";
    public const string TestPayProviderName = "TestPay";

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
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return string.Equals(value, TestPayProviderName, StringComparison.OrdinalIgnoreCase)
            ? TestPayProviderName
            : null;
    }
}
