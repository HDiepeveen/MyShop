using Microsoft.EntityFrameworkCore;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Infrastructure.Persistence.Configurations;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class PaymentOptionsRepository(MyShopDbContext context) : IPaymentOptionsRepository
{
    public async Task<PaymentOptionsSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        var row = await context.PaymentOptions.AsNoTracking().SingleAsync(
            options => options.Id == PaymentOptionsPersistenceConfiguration.SingletonId, cancellationToken);
        return new(row.PayLaterEnabled, row.OnlinePaymentEnabled, row.Version);
    }

    public async Task<PaymentOptionsSnapshot?> SaveAsync(bool payLaterEnabled, bool onlinePaymentEnabled,
        Guid expectedRevision, CancellationToken cancellationToken)
    {
        var replacement = Guid.NewGuid();
        var changed = await context.PaymentOptions
            .Where(options => options.Id == PaymentOptionsPersistenceConfiguration.SingletonId
                && options.Version == expectedRevision)
            .ExecuteUpdateAsync(update => update
                .SetProperty(options => options.PayLaterEnabled, payLaterEnabled)
                .SetProperty(options => options.OnlinePaymentEnabled, onlinePaymentEnabled)
                .SetProperty(options => options.Version, replacement), cancellationToken);
        return changed == 0 ? null : new(payLaterEnabled, onlinePaymentEnabled, replacement);
    }
}
