using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository : IOrderRepository, IOrderReadRepository, IOrderStatusRepository
{
    private readonly MyShopDbContext context;

    internal OrderRepository(MyShopDbContext context) =>
        this.context = context ?? throw new ArgumentNullException(nameof(context));

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
            Version = Guid.NewGuid(),
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

    public async Task<OrderListPage> ListAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        var totalCount = await context.Orders.AsNoTracking().CountAsync(cancellationToken);
        var rows = await context.Orders.AsNoTracking()
            .OrderByDescending(order => order.PlacedAt)
            .ThenByDescending(order => order.Id)
            .Skip(offset)
            .Take(limit)
            .Select(order => new
            {
                order.Id,
                order.Number,
                order.PlacedAt,
                order.CustomerName,
                order.PaymentMethod,
                order.Status,
                order.PaidAt
            })
            .ToArrayAsync(cancellationToken);
        var ids = rows.Select(row => row.Id).ToArray();
        var totals = await context.OrderTotals.AsNoTracking()
            .Where(total => ids.Contains(total.OrderId))
            .OrderBy(total => total.Currency)
            .Select(total => new { total.OrderId, total.Currency, total.Amount })
            .ToArrayAsync(cancellationToken);
        var totalsByOrder = totals.ToLookup(total => total.OrderId);
        var items = rows.Select(row => new OrderListItem(
            row.Id,
            row.Number,
            row.PlacedAt,
            row.CustomerName,
            (OrderPaymentMethod)row.PaymentMethod,
            (OrderStatus)row.Status,
            row.PaidAt,
            totalsByOrder[row.Id].Select(total => new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray()))
            .ToArray();
        return new OrderListPage(items, totalCount);
    }

    public async Task<OrderDetail?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await context.Orders.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Lines)
            .Include(item => item.Totals)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return order is null ? null : new OrderDetail(
            order.Id,
            order.Number,
            order.PlacedAt,
            order.CustomerName,
            order.Email,
            order.AddressLine,
            order.PostalCode,
            order.City,
            order.CountryCode,
            (OrderPaymentMethod)order.PaymentMethod,
            (OrderStatus)order.Status,
            order.PaidAt,
            order.Version,
            order.Lines.OrderBy(line => line.Ordinal).Select(line => new OrderLineSnapshot(
                line.ProductId,
                line.VariantId,
                line.ProductName,
                line.VariantName,
                line.Quantity,
                line.UnitAmount,
                line.Currency,
                line.TotalAmount)).ToArray(),
            order.Totals.OrderBy(total => total.Currency).Select(total =>
                new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray());
    }

    public async Task<OrderStatusSnapshot?> MarkPaidAsync(Guid id, Guid expectedRevision,
        DateTimeOffset paidAt, CancellationToken cancellationToken)
    {
        var replacement = Guid.NewGuid();
        var changed = await context.Orders
            .Where(order => order.Id == id
                && order.Version == expectedRevision
                && order.Status == (int)OrderStatus.AwaitingPayment)
            .ExecuteUpdateAsync(update => update
                .SetProperty(order => order.Status, (int)OrderStatus.Paid)
                .SetProperty(order => order.PaidAt, paidAt.ToUniversalTime())
                .SetProperty(order => order.Version, replacement), cancellationToken);
        return changed == 0 ? null : new(OrderStatus.Paid, paidAt.ToUniversalTime(), replacement);
    }

    public async Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Orders.AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new OrderStatusSnapshot(
                (OrderStatus)order.Status, order.PaidAt, order.Version))
            .SingleOrDefaultAsync(cancellationToken);
}
