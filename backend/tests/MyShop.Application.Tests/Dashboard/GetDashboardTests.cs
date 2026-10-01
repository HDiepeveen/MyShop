using MyShop.Application.Dashboard.Abstractions;
using MyShop.Application.Dashboard.GetDashboard;

namespace MyShop.Application.Tests.Dashboard;

public sealed class GetDashboardTests
{
    [Fact]
    public async Task ExecuteAsyncUsesOperationalDashboardLimits()
    {
        var repository = new RepositoryFake();
        using var source = new CancellationTokenSource();
        var handler = new GetDashboard(repository);

        var result = await handler.ExecuteAsync(source.Token);

        Assert.Same(repository.Snapshot, result);
        Assert.Equal(GetDashboard.LowStockThreshold, repository.LowStockThreshold);
        Assert.Equal(GetDashboard.RecentOrderLimit, repository.RecentOrderLimit);
        Assert.Equal(source.Token, repository.CancellationToken);
    }

    [Fact]
    public void RequiresRepository()
    {
        Assert.Throws<ArgumentNullException>(() => new GetDashboard(null!));
    }

    private sealed class RepositoryFake : IDashboardReadRepository
    {
        public DashboardSnapshot Snapshot { get; } = new(0, 0, 0, [], [], [], []);
        public int LowStockThreshold { get; private set; }
        public int RecentOrderLimit { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<DashboardSnapshot> GetAsync(int lowStockThreshold, int recentOrderLimit,
            CancellationToken cancellationToken)
        {
            LowStockThreshold = lowStockThreshold;
            RecentOrderLimit = recentOrderLimit;
            CancellationToken = cancellationToken;
            return Task.FromResult(Snapshot);
        }
    }
}
