using MyShop.Domain.Checkout;

namespace MyShop.Application.Dashboard.Abstractions;

public interface IDashboardReadRepository
{
    Task<DashboardSnapshot> GetAsync(int lowStockThreshold, int recentOrderLimit,
        CancellationToken cancellationToken);
}

public sealed record DashboardSnapshot(int ProductCount, int PublishedProductCount,
    int CustomerCount, IReadOnlyList<DashboardOrderStatusCount> Orders,
    IReadOnlyList<DashboardAmount> ActiveRevenue, IReadOnlyList<DashboardLowStockItem> LowStock,
    IReadOnlyList<DashboardRecentOrder> RecentOrders);
public sealed record DashboardOrderStatusCount(OrderStatus Status, int Count);
public sealed record DashboardAmount(string Currency, decimal Amount);
public sealed record DashboardLowStockItem(Guid ProductId, Guid VariantId, string ProductName,
    string VariantName, string? Sku, int Quantity);
public sealed record DashboardRecentOrder(Guid Id, string Number, DateTimeOffset PlacedAt,
    string CustomerName, OrderStatus Status, IReadOnlyList<DashboardAmount> Totals);
