using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Infrastructure.Payments;

internal sealed class TestOnlinePaymentProvider : IOnlinePaymentProvider
{
    public Task<OnlinePaymentProviderStart> StartAsync(OnlinePaymentProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(request.ProviderName, OnlinePaymentAvailability.TestPayProviderName,
                StringComparison.Ordinal))
            throw new InvalidOperationException("TestPay can only start TestPay payment requests.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PaymentReference);
        if (request.CheckoutToken == Guid.Empty)
            throw new ArgumentException("Checkout token is required.", nameof(request));
        if (request.Totals.Count == 0)
            throw new ArgumentException("At least one total is required.", nameof(request));

        var providerPaymentId = $"test_{request.CheckoutToken:N}";
        var checkoutUrl = new Uri($"https://payments.example.test/{providerPaymentId}");
        return Task.FromResult(new OnlinePaymentProviderStart(providerPaymentId, checkoutUrl));
    }
}
