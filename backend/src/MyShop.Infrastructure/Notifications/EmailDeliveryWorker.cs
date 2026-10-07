using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Notifications;

public sealed class EmailDeliveryWorker(IServiceScopeFactory scopes, IConfiguration configuration,
    IHostEnvironment environment, ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        do
        {
            try { await DeliverNextAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Email delivery failed; queued messages will be retried."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task DeliverNextAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        var settings = await scope.ServiceProvider.GetRequiredService<EmailSettingsRepository>().RuntimeAsync(cancellationToken);
        if (!settings.Public.Enabled) return;
        if (settings.Mode is not "Pickup" and not "Smtp" || (settings.Mode == "Pickup" && !environment.IsDevelopment()))
            throw new InvalidOperationException("Invalid email delivery mode.");
        var now = DateTimeOffset.UtcNow;
        var id = await context.EmailMessages.AsNoTracking().Where(message => message.SentAt == null && message.NextAttemptAt <= now
            && (message.LeaseUntil == null || message.LeaseUntil < now)).OrderBy(message => message.CreatedAt)
            .Select(message => (Guid?)message.Id).FirstOrDefaultAsync(cancellationToken);
        if (id is null) return;
        var lease = Guid.NewGuid();
        var claimed = await context.EmailMessages.Where(message => message.Id == id && message.SentAt == null && message.NextAttemptAt <= now
            && (message.LeaseUntil == null || message.LeaseUntil < now))
            .ExecuteUpdateAsync(update => update.SetProperty(message => message.Lease, lease)
                .SetProperty(message => message.LeaseUntil, now.AddMinutes(5))
                .SetProperty(message => message.Attempts, message => message.Attempts + 1), cancellationToken);
        if (claimed != 1) return;
        var queued = await context.EmailMessages.AsNoTracking().SingleAsync(message => message.Id == id, cancellationToken);
        try
        {
            using var message = new MailMessage(new MailAddress(string.IsNullOrEmpty(settings.Public.FromAddress) ? "myshop@example.invalid" : settings.Public.FromAddress, settings.Public.FromName), new MailAddress(queued.Recipient))
                { Subject = queued.Subject, Body = queued.Body, BodyEncoding = Encoding.UTF8, SubjectEncoding = Encoding.UTF8 };
            message.Headers.Add("Message-ID", $"<{queued.Id:N}@myshop>");
            using var smtp = new SmtpClient();
            if (settings.Mode == "Pickup")
            {
                var directory = Path.GetFullPath(configuration["Email:PickupDirectory"] ?? Path.Combine(environment.ContentRootPath, ".mail"));
                Directory.CreateDirectory(directory);
                smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                smtp.PickupDirectoryLocation = directory;
            }
            else
            {
                smtp.Host = settings.Public.Host;
                smtp.Port = settings.Public.Port;
                smtp.EnableSsl = true;
                if (!string.IsNullOrEmpty(settings.Public.UserName))
                    smtp.Credentials = new NetworkCredential(settings.Public.UserName, settings.Password);
            }
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            await smtp.SendMailAsync(message, timeout.Token);
            await context.EmailMessages.Where(message => message.Id == id && message.Lease == lease)
                .ExecuteUpdateAsync(update => update.SetProperty(message => message.SentAt, DateTimeOffset.UtcNow)
                    .SetProperty(message => message.Body, "")
                    .SetProperty(message => message.LeaseUntil, (DateTimeOffset?)null), cancellationToken);
        }
        catch
        {
            await context.EmailMessages.Where(message => message.Id == id && message.Lease == lease)
                .ExecuteUpdateAsync(update => update.SetProperty(message => message.NextAttemptAt, DateTimeOffset.UtcNow.AddMinutes(Math.Min(60, queued.Attempts * 2)))
                    .SetProperty(message => message.LeaseUntil, (DateTimeOffset?)null), CancellationToken.None);
            throw;
        }
    }
}
