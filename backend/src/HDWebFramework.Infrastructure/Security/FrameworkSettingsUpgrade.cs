using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Infrastructure.Notifications;
using HDWebFramework.Infrastructure.Payments;

namespace HDWebFramework.Infrastructure.Security;

public static class FrameworkSettingsUpgrade
{
    public static async Task UpgradeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<EmailSettingsRepository>().UpgradeSecretAsync(ct);
        await scope.ServiceProvider.GetRequiredService<MollieSettingsStore>().UpgradeSecretAsync(ct);
    }
}
