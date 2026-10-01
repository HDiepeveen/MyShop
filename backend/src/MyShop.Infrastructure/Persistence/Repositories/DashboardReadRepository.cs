using Microsoft.EntityFrameworkCore;
using MyShop.Application.Dashboard.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class DashboardReadRepository(MyShopDbContext context) : IDashboardReadRepository
{
    public async Task<DashboardSnapshot> GetAsync(int lowStockThreshold, int recentOrderLimit,
        CancellationToken cancellationToken)
    {
        var productCount = await context.Products.CountAsync(cancellationToken);
        var publishedProductCount = await context.Products.CountAsync(product => product.IsPublished,
            cancellationToken);
        var customerCount = await context.Users.CountAsync(user => context.UserRoles.Any(userRole =>
            userRole.UserId == user.Id && context.Roles.Any(role => role.Id == userRole.RoleId
                && role.NormalizedName == "CUSTOMER")), cancellationToken);
        var orders = await context.Orders.GroupBy(order => order.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(item => item.Status).ToArrayAsync(cancellationToken);
        var activeRevenue = await context.OrderTotals
            .Where(total => total.Order.Status == (int)OrderStatus.Paid
                || total.Order.Status == (int)OrderStatus.Shipped)
            .GroupBy(total => total.Currency)
            .Select(group => new { Currency = group.Key, Amount = group.Sum(total => total.Amount) })
            .OrderBy(item => item.Currency).ToArrayAsync(cancellationToken);
        var lowStock = await context.ProductVariants.AsNoTracking()
            .Where(variant => variant.StockQuantity != null
                && variant.Product.IsPublished
                && variant.StockQuantity <= lowStockThreshold)
            .OrderBy(variant => variant.StockQuantity).ThenBy(variant => variant.Product.Name)
            .ThenBy(variant => variant.Name).ThenBy(variant => variant.Id)
            .Select(variant => new DashboardLowStockItem(variant.ProductId, variant.Id,
                variant.Product.Name, variant.Name, variant.Sku, variant.StockQuantity!.Value))
            .Take(20).ToArrayAsync(cancellationToken);
        var recentRows = await context.Orders.AsNoTracking()
            .OrderByDescending(order => order.PlacedAt).ThenByDescending(order => order.Id)
            .Take(recentOrderLimit)
            .Select(order => new { order.Id, order.Number, order.PlacedAt, order.CustomerName, order.Status })
            .ToArrayAsync(cancellationToken);
        var recentIds = recentRows.Select(order => order.Id).ToArray();
        var totals = await context.OrderTotals.AsNoTracking().Where(total => recentIds.Contains(total.OrderId))
            .OrderBy(total => total.Currency).Select(total => new { total.OrderId, total.Currency, total.Amount })
            .ToArrayAsync(cancellationToken);
        var totalsByOrder = totals.ToLookup(total => total.OrderId);
        return new(productCount, publishedProductCount, customerCount,
            orders.Select(item => new DashboardOrderStatusCount((OrderStatus)item.Status, item.Count)).ToArray(),
            activeRevenue.Select(item => new DashboardAmount(item.Currency, item.Amount)).ToArray(),
            lowStock, recentRows.Select(item => new DashboardRecentOrder(item.Id, item.Number,
                item.PlacedAt, item.CustomerName, (OrderStatus)item.Status,
                totalsByOrder[item.Id].Select(total => new DashboardAmount(total.Currency, total.Amount)).ToArray()))
                .ToArray());
    }
}
