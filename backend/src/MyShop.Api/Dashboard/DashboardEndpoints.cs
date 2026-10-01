using System.Globalization;
using MyShop.Application.Dashboard.Abstractions;
using MyShop.Api.Security;
using MyShop.Domain.Checkout;
using UseCase = MyShop.Application.Dashboard.GetDashboard.GetDashboard;

namespace MyShop.Api.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboard(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", GetAsync).WithName("GetDashboard")
            .RequireAuthorization(policy => policy.RequireRole(AdminSecurity.Role));
        return endpoints;
    }

    private static async Task<IResult> GetAsync(UseCase useCase, CancellationToken cancellationToken)
    {
        var dashboard = await useCase.ExecuteAsync(cancellationToken);
        return Results.Ok(new DashboardResponse(dashboard.ProductCount,
            dashboard.PublishedProductCount, dashboard.ProductCount - dashboard.PublishedProductCount,
            dashboard.CustomerCount, dashboard.Orders.Select(item =>
                new DashboardStatusResponse(Status(item.Status), item.Count)).ToArray(),
            dashboard.ActiveRevenue.Select(Amount).ToArray(),
            dashboard.LowStock.Select(item => new DashboardLowStockResponse(item.ProductId,
                item.VariantId, item.ProductName, item.VariantName, item.Sku, item.Quantity)).ToArray(),
            dashboard.RecentOrders.Select(item => new DashboardOrderResponse(item.Id, item.Number,
                item.PlacedAt, item.CustomerName, Status(item.Status), item.Totals.Select(Amount).ToArray()))
                .ToArray()));
    }

    private static DashboardAmountResponse Amount(DashboardAmount amount) =>
        new(amount.Currency, amount.Amount.ToString("0.00", CultureInfo.InvariantCulture));
    private static string Status(OrderStatus status) => status switch
    {
        OrderStatus.AwaitingPayment => "awaitingPayment",
        OrderStatus.Paid => "paid",
        OrderStatus.Shipped => "shipped",
        OrderStatus.Cancelled => "cancelled",
        OrderStatus.Refunded => "refunded",
        _ => throw new InvalidOperationException($"Unsupported order status: {status}.")
    };
}

public sealed record DashboardResponse(int ProductCount, int PublishedProductCount,
    int DraftProductCount, int CustomerCount, IReadOnlyList<DashboardStatusResponse> Orders,
    IReadOnlyList<DashboardAmountResponse> ActiveRevenue,
    IReadOnlyList<DashboardLowStockResponse> LowStock,
    IReadOnlyList<DashboardOrderResponse> RecentOrders);
public sealed record DashboardStatusResponse(string Status, int Count);
public sealed record DashboardAmountResponse(string Currency, string Amount);
public sealed record DashboardLowStockResponse(Guid ProductId, Guid VariantId, string ProductName,
    string VariantName, string? Sku, int Quantity);
public sealed record DashboardOrderResponse(Guid Id, string Number, DateTimeOffset PlacedAt,
    string CustomerName, string Status, IReadOnlyList<DashboardAmountResponse> Totals);
