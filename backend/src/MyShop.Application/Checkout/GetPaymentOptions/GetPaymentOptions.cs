using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Application.Checkout.GetPaymentOptions;

public sealed record PaymentOption(string Code, string Name, string? Instructions);
public sealed record PublicPaymentOptions(IReadOnlyList<PaymentOption> Items);

public sealed class GetPaymentOptions(IPaymentOptionsRepository repository, IOnlinePaymentAvailability online)
{
    private readonly IPaymentOptionsRepository repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IOnlinePaymentAvailability online = online ?? throw new ArgumentNullException(nameof(online));

    public async Task<PublicPaymentOptions> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = await repository.GetAsync(cancellationToken);
        var items = new List<PaymentOption>();
        if (settings.PayLaterEnabled)
            items.Add(new("payLater", "Later betalen", settings.PayLaterInstructions));
        if (settings.OnlinePaymentEnabled && online.IsConfigured)
            items.Add(new("online", "Direct online betalen",
                online.ProviderName is null
                    ? "Je wordt doorgestuurd naar de betaalprovider."
                    : $"Je wordt doorgestuurd naar {online.ProviderName}."));
        return new(items);
    }
}
