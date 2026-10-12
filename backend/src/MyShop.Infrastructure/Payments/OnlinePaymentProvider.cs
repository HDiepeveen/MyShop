using Microsoft.Extensions.Configuration;
using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Infrastructure.Payments;

internal sealed class OnlinePaymentProvider(IHttpClientFactory http, IConfiguration configuration, MollieSettingsStore? managed = null)
    : IOnlinePaymentProvider, IOnlinePaymentStatusReader
{
    public async Task<OnlinePaymentProviderStart> StartAsync(OnlinePaymentProviderRequest request, CancellationToken cancellationToken) =>
        request.ProviderName switch
        {
            "TestPay" => await new TestOnlinePaymentProvider().StartAsync(request, cancellationToken),
            "Mollie" => await (await Mollie(cancellationToken)).StartAsync(request, cancellationToken),
            _ => throw new OnlinePaymentProviderException()
        };

    public async Task<OnlinePaymentVerification> VerifyAsync(OnlinePaymentStartRecord payment, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (payment.ProviderName == "Mollie") return await (await Mollie(cancellationToken)).VerifyAsync(payment, cancellationToken);
        if (payment.ProviderName == "TestPay")
            return new OnlinePaymentVerification(OnlinePaymentStatus.Paid,
                payment.ProviderPaymentId == $"test_{payment.CheckoutToken:N}");
        throw new OnlinePaymentProviderException();
    }

    private async Task<MollieOnlinePaymentProvider> Mollie(CancellationToken ct) => new(http.CreateClient("Mollie"), managed is null ? new(configuration) : (await managed.RuntimeAsync(ct)).Runtime);
}
