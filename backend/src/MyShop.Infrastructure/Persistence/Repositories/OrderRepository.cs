using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(MyShopDbContext context) : IOrderRepository
{
    public async Task<OrderReceipt?> GetByCheckoutTokenAsync(Guid checkoutToken,
        CancellationToken cancellationToken) => await context.Orders.AsNoTracking()
            .Where(order => order.CheckoutToken == checkoutToken)
            .Select(order => new OrderReceipt(order.Id, order.Number, order.PlacedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<OrderReceipt> AddAsync(Order order, Guid checkoutToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (checkoutToken == Guid.Empty) throw new ArgumentException("Checkout token is required.", nameof(checkoutToken));
        var persistence = new OrderPersistence
        {
            Id = order.Id,
            CheckoutToken = checkoutToken,
            Number = order.Number,
            PlacedAt = order.PlacedAt,
            CustomerName = order.Customer.Name,
            Email = order.Customer.Email,
            AddressLine = order.DeliveryAddress.AddressLine,
            PostalCode = order.DeliveryAddress.PostalCode,
            City = order.DeliveryAddress.City,
            CountryCode = order.DeliveryAddress.CountryCode,
            PaymentMethod = (int)order.PaymentMethod,
            Status = (int)order.Status,
            Lines = order.Lines.Select((line, index) => new OrderLinePersistence
            {
                OrderId = order.Id,
                Ordinal = index,
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                ProductName = line.ProductName,
                VariantName = line.VariantName,
                Quantity = line.Quantity,
                UnitAmount = line.UnitPrice.Amount,
                Currency = line.UnitPrice.Currency,
                TotalAmount = line.Total.Amount
            }).ToArray(),
            Totals = order.Totals.Select(total => new OrderTotalPersistence
            {
                OrderId = order.Id,
                Currency = total.Currency,
                Amount = total.Amount
            }).ToArray()
        };
        context.Orders.Add(persistence);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added).ToArray())
                entry.State = EntityState.Detached;
            return await GetByCheckoutTokenAsync(checkoutToken, cancellationToken)
                ?? throw new InvalidOperationException("The order conflict could not be resolved.", exception);
        }
        return new(order.Id, order.Number, order.PlacedAt);
    }
}
