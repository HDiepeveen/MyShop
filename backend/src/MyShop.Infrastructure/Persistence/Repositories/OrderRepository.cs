using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Customers.Abstractions;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository : IOrderRepository, IOrderReadRepository, IOrderStatusRepository,
    ICustomerOrderReadRepository, ICustomerOrderCancellationRepository
{
    private readonly MyShopDbContext context;

    internal OrderRepository(MyShopDbContext context) =>
        this.context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<OrderReceipt?> GetByCheckoutTokenAsync(Guid checkoutToken,
        CancellationToken cancellationToken) => await context.Orders.AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Totals)
            .Where(order => order.CheckoutToken == checkoutToken)
            .Select(order => new OrderReceipt(order.Id, order.Number, order.PlacedAt,
                order.PaymentInstructions,
                order.Totals.OrderBy(total => total.Currency).Select(total =>
                    new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray(),
                Delivery(order)))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<OrderReceipt?> AddAsync(Order order, Guid checkoutToken, string? customerUserId,
        IReadOnlyList<StockReservation> stock, CancellationToken cancellationToken) =>
        AddAsync(order, checkoutToken, customerUserId, stock, null, null, cancellationToken);

    public Task<OrderReceipt?> AddPaidAsync(Order order, Guid checkoutToken, string paymentReference,
        IReadOnlyList<StockReservation> stock, CancellationToken cancellationToken)
    {
        if (order.PaymentMethod != OrderPaymentMethod.Online)
            throw new ArgumentException("Paid orders must use online payment.", nameof(order));
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentReference);
        paymentReference = paymentReference.Trim();
        if (paymentReference.Length > 100)
            throw new ArgumentException("Payment reference must contain at most 100 characters.", nameof(paymentReference));
        return AddAsync(order, checkoutToken, null, stock, DateTimeOffset.UtcNow, paymentReference,
            cancellationToken);
    }

    private async Task<OrderReceipt?> AddAsync(Order order, Guid checkoutToken, string? customerUserId,
        IReadOnlyList<StockReservation> stock, DateTimeOffset? paidAt, string? paymentReference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(stock);
        if (checkoutToken == Guid.Empty) throw new ArgumentException("Checkout token is required.", nameof(checkoutToken));
        if (customerUserId is not null && string.IsNullOrWhiteSpace(customerUserId))
            throw new ArgumentException("Customer user ID must not be blank.", nameof(customerUserId));
        if (stock.Any(item => item.ProductId == Guid.Empty || item.VariantId == Guid.Empty
                || item.Quantity < 1)
            || stock.Select(item => (item.ProductId, item.VariantId)).Distinct().Count() != stock.Count)
            throw new ArgumentException("Stock reservations must contain unique valid variants.", nameof(stock));
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var reservation in stock)
        {
            var quantity = reservation.Quantity;
            var changed = await context.ProductVariants
                .Where(variant => variant.Id == reservation.VariantId
                    && variant.ProductId == reservation.ProductId
                    && variant.Product.IsPublished
                    && variant.StockQuantity != null
                    && variant.StockQuantity >= quantity)
                .ExecuteUpdateAsync(update => update.SetProperty(variant => variant.StockQuantity,
                    variant => variant.StockQuantity!.Value - quantity), cancellationToken);
            if (changed == 1)
            {
                await context.Products.Where(product => product.Id == reservation.ProductId)
                    .ExecuteUpdateAsync(update => update.SetProperty(product => product.Version,
                        Guid.NewGuid()), cancellationToken);
                continue;
            }
            var isUntracked = await context.ProductVariants.AnyAsync(variant =>
                variant.Id == reservation.VariantId
                && variant.ProductId == reservation.ProductId
                && variant.Product.IsPublished
                && variant.StockQuantity == null, cancellationToken);
            if (!isUntracked)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
        }
        var persistence = new OrderPersistence
        {
            Id = order.Id,
            CheckoutToken = checkoutToken,
            CustomerUserId = customerUserId,
            Number = order.Number,
            PlacedAt = order.PlacedAt,
            CustomerName = order.Customer.Name,
            Email = order.Customer.Email,
            AddressLine = order.DeliveryAddress.AddressLine,
            PostalCode = order.DeliveryAddress.PostalCode,
            City = order.DeliveryAddress.City,
            CountryCode = order.DeliveryAddress.CountryCode,
            DeliveryMethodId = order.DeliveryMethod?.Id,
            DeliveryMethodName = order.DeliveryMethod?.Name,
            DeliveryDescription = order.DeliveryMethod?.Description,
            DeliveryAmount = order.DeliveryMethod?.Fee.Amount,
            DeliveryCurrency = order.DeliveryMethod?.Fee.Currency,
            PaymentMethod = (int)order.PaymentMethod,
            PaymentInstructions = order.PaymentInstructions,
            Status = (int)(paidAt is null ? order.Status : OrderStatus.Paid),
            PaidAt = paidAt?.ToUniversalTime(),
            PaymentReference = paymentReference,
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
                TotalAmount = line.Total.Amount,
                VatRate = line.VatRate, VatExempt = line.VatExempt, NetAmount = line.NetAmount, VatAmount = line.VatAmount
            }).ToArray(),
            Totals = order.Totals.Select(total => new OrderTotalPersistence
            {
                OrderId = order.Id,
                Currency = total.Currency,
                Amount = total.Amount
            }).ToArray()
        };
        context.Orders.Add(persistence);
        context.EmailMessages.Add(MyShop.Infrastructure.Notifications.EmailQueue.Create(order.Customer.Email,
            $"MyShop bestelbevestiging {order.Number}", MyShop.Infrastructure.Notifications.OrderConfirmationEmail.Body(order)));
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(cancellationToken);
            foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added).ToArray())
                entry.State = EntityState.Detached;
            return await GetByCheckoutTokenAsync(checkoutToken, cancellationToken)
                ?? throw new InvalidOperationException("The order conflict could not be resolved.", exception);
        }
        return new(order.Id, order.Number, order.PlacedAt, order.PaymentInstructions,
            order.Totals.OrderBy(total => total.Currency).Select(total =>
                new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray(),
            order.DeliveryMethod is null ? null : new OrderDeliveryMethodSnapshot(order.DeliveryMethod.Id,
                order.DeliveryMethod.Name, order.DeliveryMethod.Description, order.DeliveryMethod.Fee.Amount,
                order.DeliveryMethod.Fee.Currency));
    }

    public async Task<OrderListPage> ListAsync(int offset, int limit, OrderStatus? status, string? search,
        CancellationToken cancellationToken)
    {
        var query = context.Orders.AsNoTracking();
        if (status is not null)
            query = query.Where(order => order.Status == (int)status.Value);
        if (search is not null)
            query = query.Where(order => order.Number.Contains(search)
                || order.CustomerName.Contains(search)
                || order.Email.Contains(search));
        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
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
                order.PaidAt,
                order.ShippedAt,
                order.CancelledAt,
                order.CancellationReason,
                order.RefundedAt,
                order.RefundReason
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
            row.ShippedAt,
            row.CancelledAt,
            row.CancellationReason,
            row.RefundedAt,
            row.RefundReason,
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
            order.PaymentInstructions,
            (OrderStatus)order.Status,
            order.PaidAt,
            order.PaymentReference,
            order.ShippedAt,
            order.ShippingCarrier,
            order.TrackingCode,
            order.CancelledAt,
            order.CancellationReason,
            order.RefundedAt,
            order.RefundReference,
            order.RefundReason,
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
                new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray(),
            Delivery(order));
    }

    public async Task<CustomerOrderPage> ListAsync(string customerUserId, int offset, int limit,
        CancellationToken cancellationToken, OrderStatus? status = null, string? search = null)
    {
        var query = context.Orders.AsNoTracking()
            .Where(order => order.CustomerUserId == customerUserId);
        if (status is not null) query = query.Where(order => order.Status == (int)status.Value);
        if (search is not null) query = query.Where(order => order.Number.Contains(search));
        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(order => order.PlacedAt)
            .ThenByDescending(order => order.Id)
            .Skip(offset).Take(limit)
            .Select(order => new
            {
                order.Id, order.Number, order.PlacedAt, order.PaymentMethod, order.Status,
                order.ShippedAt, order.ShippingCarrier, order.TrackingCode
            })
            .ToArrayAsync(cancellationToken);
        var ids = rows.Select(row => row.Id).ToArray();
        var totals = await context.OrderTotals.AsNoTracking()
            .Where(total => ids.Contains(total.OrderId))
            .OrderBy(total => total.Currency)
            .Select(total => new { total.OrderId, total.Currency, total.Amount })
            .ToArrayAsync(cancellationToken);
        var totalsByOrder = totals.ToLookup(total => total.OrderId);
        return new(rows.Select(row => new CustomerOrderListItem(row.Id, row.Number, row.PlacedAt,
            (OrderPaymentMethod)row.PaymentMethod, (OrderStatus)row.Status, row.ShippedAt,
            row.ShippingCarrier, row.TrackingCode,
            totalsByOrder[row.Id].Select(total =>
                new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray())).ToArray(), totalCount);
    }

    public async Task<CustomerOrderDetail?> GetAsync(string customerUserId, Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await context.Orders.AsNoTracking().AsSplitQuery()
            .Include(item => item.Lines).Include(item => item.Totals)
            .SingleOrDefaultAsync(item => item.Id == orderId
                && item.CustomerUserId == customerUserId, cancellationToken);
        return order is null ? null : new CustomerOrderDetail(order.Id, order.Number, order.PlacedAt,
            order.CustomerName, order.Email, order.AddressLine, order.PostalCode, order.City,
            order.CountryCode, (OrderPaymentMethod)order.PaymentMethod, order.PaymentInstructions,
            (OrderStatus)order.Status, order.PaidAt, order.ShippedAt, order.ShippingCarrier,
            order.TrackingCode, order.CancelledAt, order.RefundedAt, order.Version,
            order.Lines.OrderBy(line => line.Ordinal).Select(line => new OrderLineSnapshot(
                line.ProductId, line.VariantId, line.ProductName, line.VariantName, line.Quantity,
                line.UnitAmount, line.Currency, line.TotalAmount)).ToArray(),
            order.Totals.OrderBy(total => total.Currency).Select(total =>
                new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray(), Delivery(order));
    }

    private static OrderDeliveryMethodSnapshot? Delivery(OrderPersistence order) =>
        order.DeliveryMethodId is null || order.DeliveryMethodName is null
            || order.DeliveryAmount is null || order.DeliveryCurrency is null
            ? null
            : new(order.DeliveryMethodId.Value, order.DeliveryMethodName,
                order.DeliveryDescription, order.DeliveryAmount.Value, order.DeliveryCurrency);

    public async Task<Guid?> MarkPaidAsync(Guid id, Guid expectedRevision,
        DateTimeOffset paidAt, string paymentReference, CancellationToken cancellationToken)
    {
        var replacement = Guid.NewGuid();
        var changed = await context.Orders
            .Where(order => order.Id == id
                && order.Version == expectedRevision
                && order.Status == (int)OrderStatus.AwaitingPayment)
            .ExecuteUpdateAsync(update => update
                .SetProperty(order => order.Status, (int)OrderStatus.Paid)
                .SetProperty(order => order.PaidAt, paidAt.ToUniversalTime())
                .SetProperty(order => order.PaymentReference, paymentReference)
                .SetProperty(order => order.Version, replacement), cancellationToken);
        return changed == 0 ? null : replacement;
    }

    public async Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Orders.AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new OrderStatusSnapshot(
                (OrderStatus)order.Status, order.PaidAt, order.PaymentReference, order.ShippedAt,
                order.ShippingCarrier, order.TrackingCode,
                order.CancelledAt, order.CancellationReason, order.RefundedAt,
                order.RefundReference, order.RefundReason, order.Version))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Guid?> MarkShippedAsync(Guid id, Guid expectedRevision,
        DateTimeOffset shippedAt, string carrier, string trackingCode,
        CancellationToken cancellationToken)
    {
        var replacement = Guid.NewGuid();
        var changed = await context.Orders
            .Where(order => order.Id == id
                && order.Version == expectedRevision
                && order.Status == (int)OrderStatus.Paid)
            .ExecuteUpdateAsync(update => update
                .SetProperty(order => order.Status, (int)OrderStatus.Shipped)
                .SetProperty(order => order.ShippedAt, shippedAt.ToUniversalTime())
                .SetProperty(order => order.ShippingCarrier, carrier)
                .SetProperty(order => order.TrackingCode, trackingCode)
                .SetProperty(order => order.Version, replacement), cancellationToken);
        return changed == 0 ? null : replacement;
    }

    public async Task<Guid?> CancelAsync(Guid id, Guid expectedRevision, DateTimeOffset cancelledAt,
        string reason, CancellationToken cancellationToken) => await CancelAsync(id, null,
            expectedRevision, cancelledAt, reason, cancellationToken);

    public async Task<Guid?> CancelAsync(string customerUserId, Guid orderId, Guid expectedRevision,
        DateTimeOffset cancelledAt, string reason, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerUserId);
        return await CancelAsync(orderId, customerUserId, expectedRevision, cancelledAt, reason,
            cancellationToken);
    }

    private async Task<Guid?> CancelAsync(Guid id, string? customerUserId, Guid expectedRevision,
        DateTimeOffset cancelledAt, string reason, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var lines = await context.OrderLines.AsNoTracking()
            .Where(line => line.OrderId == id
                && (customerUserId == null || line.Order.CustomerUserId == customerUserId))
            .Select(line => new { line.ProductId, line.VariantId, line.Quantity })
            .ToArrayAsync(cancellationToken);
        var replacement = Guid.NewGuid();
        var changed = await context.Orders
            .Where(order => order.Id == id
                && order.Version == expectedRevision
                && order.Status == (int)OrderStatus.AwaitingPayment
                && (customerUserId == null || order.CustomerUserId == customerUserId))
            .ExecuteUpdateAsync(update => update
                .SetProperty(order => order.Status, (int)OrderStatus.Cancelled)
                .SetProperty(order => order.CancelledAt, cancelledAt.ToUniversalTime())
                .SetProperty(order => order.CancellationReason, reason)
                .SetProperty(order => order.Version, replacement), cancellationToken);
        if (changed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }
        foreach (var line in lines)
        {
            var quantity = line.Quantity;
            var restored = await context.ProductVariants
                .Where(variant => variant.Id == line.VariantId && variant.ProductId == line.ProductId
                    && variant.StockQuantity != null)
                .ExecuteUpdateAsync(update => update.SetProperty(variant => variant.StockQuantity,
                    variant => variant.StockQuantity!.Value > int.MaxValue - quantity
                        ? int.MaxValue
                        : variant.StockQuantity.Value + quantity), cancellationToken);
            if (restored == 1)
                await context.Products.Where(product => product.Id == line.ProductId)
                    .ExecuteUpdateAsync(update => update.SetProperty(product => product.Version,
                        Guid.NewGuid()), cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return replacement;
    }

    public async Task<Guid?> RefundAsync(Guid id, Guid expectedRevision, DateTimeOffset refundedAt,
        string refundReference, string reason, CancellationToken cancellationToken)
    {
        var replacement = Guid.NewGuid();
        var changed = await context.Orders
            .Where(order => order.Id == id
                && order.Version == expectedRevision
                && order.Status == (int)OrderStatus.Paid)
            .ExecuteUpdateAsync(update => update
                .SetProperty(order => order.Status, (int)OrderStatus.Refunded)
                .SetProperty(order => order.RefundedAt, refundedAt.ToUniversalTime())
                .SetProperty(order => order.RefundReference, refundReference)
                .SetProperty(order => order.RefundReason, reason)
                .SetProperty(order => order.Version, replacement), cancellationToken);
        return changed == 0 ? null : replacement;
    }
}
