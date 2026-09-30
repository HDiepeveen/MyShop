using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Application.Checkout.GetAdminPaymentOptions;

public sealed record AdminPaymentOptions(bool PayLaterEnabled, bool OnlinePaymentEnabled,
    bool OnlinePaymentConfigured, Guid Revision);

public sealed class GetAdminPaymentOptions(
    IPaymentOptionsRepository repository,
    IOnlinePaymentAvailability online)
{
    private readonly IPaymentOptionsRepository repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IOnlinePaymentAvailability online = online
        ?? throw new ArgumentNullException(nameof(online));

    public async Task<AdminPaymentOptions> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = await repository.GetAsync(cancellationToken);
        return new(settings.PayLaterEnabled, settings.OnlinePaymentEnabled,
            online.IsConfigured, settings.Revision);
    }
}
