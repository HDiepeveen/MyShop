using Microsoft.EntityFrameworkCore;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class OnlinePaymentStartRepository(MyShopDbContext context) : IOnlinePaymentStartRepository
{
    public async Task<OnlinePaymentStartRecord?> GetByCheckoutTokenAsync(Guid checkoutToken,
        CancellationToken cancellationToken)
    {
        if (checkoutToken == Guid.Empty)
            throw new ArgumentException("Checkout token is required.", nameof(checkoutToken));
        var payment = await context.OnlinePaymentStarts.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Totals)
            .SingleOrDefaultAsync(item => item.CheckoutToken == checkoutToken, cancellationToken);
        return payment is null ? null : Map(payment);
    }

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

    private static OnlinePaymentStartRecord Map(OnlinePaymentStartPersistence payment) =>
        new(payment.CheckoutToken, payment.ProviderName, payment.PaymentReference,
            payment.ProviderPaymentId, new Uri(payment.CheckoutUrl),
            payment.Totals.OrderBy(total => total.Currency, StringComparer.Ordinal)
                .Select(total => new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray(),
            new OrderDeliveryMethodSnapshot(payment.DeliveryMethodId, payment.DeliveryMethodName,
                payment.DeliveryDescription, payment.DeliveryAmount, payment.DeliveryCurrency),
            payment.CreatedAt);
}
