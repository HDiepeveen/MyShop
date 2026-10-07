using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MyShop.Application.Notifications;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Configurations;

namespace MyShop.Infrastructure.Notifications;

internal sealed record EmailRuntimeSettings(EmailSettingsSnapshot Public, string Mode, string? Password);
internal sealed class EmailSettingsRepository(MyShopDbContext context, IConfiguration configuration,
    IHostEnvironment environment, IDataProtectionProvider protection) : IEmailSettingsRepository
{
    private readonly IDataProtector protector = protection.CreateProtector("MyShop.Email.SmtpPassword.v1");
    public async Task<EmailSettingsSnapshot> GetAsync(CancellationToken cancellationToken) =>
        (await LoadAsync(false, cancellationToken)).Public;
    internal Task<EmailRuntimeSettings> RuntimeAsync(CancellationToken cancellationToken) => LoadAsync(true, cancellationToken);
    private async Task<EmailRuntimeSettings> LoadAsync(bool secret, CancellationToken cancellationToken)
    {
        var row = await context.EmailSettings.AsNoTracking().SingleAsync(row => row.Id == EmailSettingsPersistenceConfiguration.Id, cancellationToken);
        if (!row.Managed)
        {
            var password = configuration["Email:Smtp:Password"];
            return new(new(configuration.GetValue<bool>("Email:Enabled"), configuration["Email:Smtp:Host"] ?? "",
                configuration.GetValue("Email:Smtp:Port", 587), configuration["Email:Smtp:User"] ?? "",
                configuration["Email:From"] ?? "", configuration["Email:FromName"] ?? "",
                configuration["Email:PublicBaseUrl"] ?? (environment.IsDevelopment() ? "http://127.0.0.1:4200" : ""),
                !string.IsNullOrEmpty(password), row.Version), configuration["Email:Mode"] ?? "Pickup", secret ? password : null);
        }
        return new(new(row.Enabled, row.Host, row.Port, row.UserName, row.FromAddress, row.FromName, row.PublicBaseUrl,
            !string.IsNullOrEmpty(row.ProtectedPassword), row.Version), "Smtp",
            secret && row.Enabled && row.ProtectedPassword is not null ? protector.Unprotect(row.ProtectedPassword) : null);
    }
    public async Task<EmailSettingsSnapshot?> SaveAsync(UpdateEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        var current = await context.EmailSettings.AsNoTracking().SingleAsync(row => row.Id == EmailSettingsPersistenceConfiguration.Id, cancellationToken);
        var password = command.ClearPassword ? null : !string.IsNullOrEmpty(command.Password) ? protector.Protect(command.Password)
            : current.Managed ? current.ProtectedPassword
            : string.IsNullOrEmpty(configuration["Email:Smtp:Password"]) ? null : protector.Protect(configuration["Email:Smtp:Password"]!);
        var version = Guid.NewGuid();
        var changed = await context.EmailSettings.Where(row => row.Id == current.Id && row.Version == command.Revision)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Managed, true)
                .SetProperty(row => row.Enabled, command.Enabled).SetProperty(row => row.Host, command.Host)
                .SetProperty(row => row.Port, command.Port).SetProperty(row => row.UserName, command.UserName)
                .SetProperty(row => row.FromAddress, command.FromAddress).SetProperty(row => row.FromName, command.FromName)
                .SetProperty(row => row.PublicBaseUrl, command.PublicBaseUrl).SetProperty(row => row.ProtectedPassword, password)
                .SetProperty(row => row.Version, version), cancellationToken);
        return changed == 0 ? null : new(command.Enabled, command.Host, command.Port, command.UserName, command.FromAddress,
            command.FromName, command.PublicBaseUrl, password is not null, version);
    }
}
