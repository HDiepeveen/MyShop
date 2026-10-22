using Microsoft.Extensions.Configuration;
using HDWebFramework.Application.Checkout.Abstractions;

namespace HDWebFramework.Infrastructure.Payments;

internal sealed class OnlinePaymentAvailability : IOnlinePaymentAvailability
{
    public const string ProviderKey = "Payments:Online:Provider";
    public const string TestPayProviderName = "TestPay";

    public OnlinePaymentAvailability(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ProviderName = string.Equals(configuration[ProviderKey]?.Trim(), "Mollie", StringComparison.OrdinalIgnoreCase)
            ? (new MolliePaymentConfiguration(configuration).IsConfigured ? "Mollie" : null)
            : Normalize(configuration[ProviderKey]);
    }

    private MollieSettingsStore? managed;
    internal OnlinePaymentAvailability(IConfiguration configuration, MollieSettingsStore managed) : this(configuration) { this.managed = managed; }
    public async Task<OnlinePaymentAvailabilitySnapshot> GetAsync(CancellationToken ct)
    {
        if (managed is null) return new(ProviderName);
        var settings = await managed.RuntimeAsync(ct);
        return new(settings.Public.Managed ? settings.Runtime.IsConfigured ? "Mollie" : null : ProviderName);
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
