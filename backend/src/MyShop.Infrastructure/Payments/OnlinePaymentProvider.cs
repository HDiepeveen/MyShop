using Microsoft.Extensions.Configuration;
using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Infrastructure.Payments;

internal sealed class OnlinePaymentProvider(IHttpClientFactory http, IConfiguration configuration)
    : IOnlinePaymentProvider, IOnlinePaymentStatusReader
{
    public Task<OnlinePaymentProviderStart> StartAsync(OnlinePaymentProviderRequest request, CancellationToken cancellationToken) =>
        request.ProviderName switch
        {
            "TestPay" => new TestOnlinePaymentProvider().StartAsync(request, cancellationToken),
            "Mollie" => Mollie().StartAsync(request, cancellationToken),
            _ => throw new OnlinePaymentProviderException()
        };

    public Task<OnlinePaymentVerification> VerifyAsync(OnlinePaymentStartRecord payment, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (payment.ProviderName == "Mollie") return Mollie().VerifyAsync(payment, cancellationToken);
        if (payment.ProviderName == "TestPay")
            return Task.FromResult(new OnlinePaymentVerification(OnlinePaymentStatus.Paid,
                payment.ProviderPaymentId == $"test_{payment.CheckoutToken:N}"));
        throw new OnlinePaymentProviderException();
    }

    private MollieOnlinePaymentProvider Mollie() => new(http.CreateClient("Mollie"), new(configuration));
}
