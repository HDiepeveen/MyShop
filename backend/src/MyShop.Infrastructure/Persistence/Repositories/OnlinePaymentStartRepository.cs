using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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
            .Include(item => item.Lines)
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
            CustomerUserId = payment.CustomerUserId,
            CustomerName = payment.Customer.Name,
            Email = payment.Customer.Email,
            AddressLine = payment.Address.AddressLine,
            PostalCode = payment.Address.PostalCode,
            City = payment.Address.City,
            CountryCode = payment.Address.CountryCode,
            CreatedAt = payment.CreatedAt.ToUniversalTime(),
            DeliveryMethodId = payment.DeliveryMethod.Id,
            DeliveryMethodName = payment.DeliveryMethod.Name,
            DeliveryDescription = payment.DeliveryMethod.Description,
            DeliveryAmount = payment.DeliveryMethod.Amount,
            DeliveryCurrency = payment.DeliveryMethod.Currency,
            Lines = payment.Lines.Select((line, index) => new OnlinePaymentStartLinePersistence
            {
                OnlinePaymentStartId = payment.CheckoutToken,
                Ordinal = index,
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                ProductName = line.ProductName,
                VariantName = line.VariantName,
                Quantity = line.Quantity,
                UnitAmount = line.UnitAmount,
                Currency = line.Currency,
                TotalAmount = line.TotalAmount,
                VatRate = line.VatRate, VatExempt = line.VatExempt
            }).ToArray(),
            Totals = payment.Totals.Select(total => new OnlinePaymentStartTotalPersistence
            {
                OnlinePaymentStartId = payment.CheckoutToken,
                Currency = total.Currency,
                Amount = total.Amount
            }).ToArray()
        });
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added).ToArray())
                entry.State = EntityState.Detached;
            var existing = await GetByCheckoutTokenAsync(payment.CheckoutToken, cancellationToken);
            if (existing?.ProviderPaymentId != payment.ProviderPaymentId) throw;
        }
    }

    private static OnlinePaymentStartRecord Map(OnlinePaymentStartPersistence payment) =>
        new(payment.CheckoutToken, payment.ProviderName, payment.PaymentReference,
            payment.ProviderPaymentId, new Uri(payment.CheckoutUrl),
            new(payment.CustomerName, payment.Email),
            new(payment.AddressLine, payment.PostalCode, payment.City, payment.CountryCode),
            payment.Lines.OrderBy(line => line.Ordinal).Select(line =>
                new OnlinePaymentStartLineSnapshot(line.ProductId, line.VariantId,
                    line.ProductName, line.VariantName, line.Quantity, line.UnitAmount,
                    line.Currency, line.TotalAmount, line.VatRate, line.VatExempt)).ToArray(),
            payment.Totals.OrderBy(total => total.Currency, StringComparer.Ordinal)
                .Select(total => new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray(),
            new OrderDeliveryMethodSnapshot(payment.DeliveryMethodId, payment.DeliveryMethodName,
                payment.DeliveryDescription, payment.DeliveryAmount, payment.DeliveryCurrency),
            payment.CreatedAt, payment.CustomerUserId);
}
