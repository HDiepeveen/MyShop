using MyShop.Application.Checkout.Abstractions;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class OnlinePaymentStartRepository(MyShopDbContext context) : IOnlinePaymentStartRepository
{
    public async Task SaveAsync(OnlinePaymentStartRecord payment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);
        cancellationToken.ThrowIfCancellationRequested();
        if (payment.CheckoutToken == Guid.Empty)
            throw new ArgumentException("Checkout token is required.", nameof(payment));

        context.OnlinePaymentStarts.Add(new OnlinePaymentStartPersistence
        {
            CheckoutToken = payment.CheckoutToken,
            ProviderName = payment.ProviderName,
            PaymentReference = payment.PaymentReference,
            ProviderPaymentId = payment.ProviderPaymentId,
            CheckoutUrl = payment.CheckoutUrl.ToString(),
            CreatedAt = payment.CreatedAt.ToUniversalTime(),
            DeliveryMethodId = payment.DeliveryMethod.Id,
            DeliveryMethodName = payment.DeliveryMethod.Name,
            DeliveryDescription = payment.DeliveryMethod.Description,
            DeliveryAmount = payment.DeliveryMethod.Amount,
            DeliveryCurrency = payment.DeliveryMethod.Currency,
            Totals = payment.Totals.Select(total => new OnlinePaymentStartTotalPersistence
            {
                OnlinePaymentStartId = payment.CheckoutToken,
                Currency = total.Currency,
                Amount = total.Amount
            }).ToArray()
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}