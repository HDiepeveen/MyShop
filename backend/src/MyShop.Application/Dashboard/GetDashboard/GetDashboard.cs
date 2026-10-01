using MyShop.Application.Dashboard.Abstractions;

namespace MyShop.Application.Dashboard.GetDashboard;

public sealed class GetDashboard(IDashboardReadRepository repository)
{
    public const int LowStockThreshold = 5;
    public const int RecentOrderLimit = 5;
    private readonly IDashboardReadRepository repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    public Task<DashboardSnapshot> ExecuteAsync(CancellationToken cancellationToken) =>
        repository.GetAsync(LowStockThreshold, RecentOrderLimit, cancellationToken);
}
