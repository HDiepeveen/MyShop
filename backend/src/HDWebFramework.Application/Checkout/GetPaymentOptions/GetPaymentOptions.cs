using HDWebFramework.Application.Checkout.Abstractions;

namespace HDWebFramework.Application.Checkout.GetPaymentOptions;

public sealed record PaymentOption(string Code, string Name, string? Instructions);
public sealed record PublicPaymentOptions(IReadOnlyList<PaymentOption> Items)
{
    public bool CheckoutEnabled { get; init; } = true;
}

public sealed class GetPaymentOptions(IPaymentOptionsRepository repository, IOnlinePaymentAvailability online)
{
    private readonly IPaymentOptionsRepository repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IOnlinePaymentAvailability online = online ?? throw new ArgumentNullException(nameof(online));

    public async Task<PublicPaymentOptions> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var onlineStatus = await online.GetAsync(cancellationToken);
        var settings = await repository.GetAsync(cancellationToken);
        if (!settings.CheckoutEnabled) return new([]) { CheckoutEnabled = false };
        var items = new List<PaymentOption>();
        if (settings.PayLaterEnabled)
            items.Add(new("payLater", "Later betalen", settings.PayLaterInstructions));
        if (settings.OnlinePaymentEnabled && onlineStatus.IsConfigured)
            items.Add(new("online", onlineStatus.ProviderName == "Mollie" ? "iDEAL" : "Direct online betalen",
                onlineStatus.ProviderName == "Mollie" ? "iDEAL-testbetaling via Mollie. Er wordt geen echt geld afgeschreven." : onlineStatus.ProviderName is null
                    ? "Je wordt doorgestuurd naar de betaalprovider."
                    : $"Je wordt doorgestuurd naar {onlineStatus.ProviderName}."));
        return new(items);
    }
}
