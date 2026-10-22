using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using HDWebFramework.Domain.Catalog;
using HDWebFramework.Domain.Checkout;
using HDWebFramework.Infrastructure.Notifications;
using HDWebFramework.Infrastructure.Persistence;
using HDWebFramework.Infrastructure.Persistence.Repositories;

namespace HDWebFramework.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class EmailDeliveryTests(SqlServerDatabase database)
{
    [Theory]
    [InlineData("victim@example.test\r\nBcc: other@example.test", "Subject")]
    [InlineData("Customer <victim@example.test>", "Subject")]
    [InlineData("victim@example.test", "Subject\nInjected")]
    public void Rejects_invalid_mail_headers(string recipient, string subject) =>
        Assert.ThrowsAny<ArgumentException>(() => EmailQueue.Create(recipient, subject, "Body"));

    [SqlServerFact]
    public async Task Order_and_email_are_atomic_and_a_repeated_checkout_creates_only_one_message()
    {
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        var order = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow, OrderCustomer.Create("Customer", email),
            DeliveryAddress.Create("Street 1", "1234 AB", "Utrecht", "NL"),
            [(Guid.NewGuid(), Guid.NewGuid(), "Saved product", "Saved variant", 2, Money.Create(10m, "EUR"))], "Saved instructions");
        var token = Guid.NewGuid();
        await using (var context = database.CreateContext())
            Assert.NotNull(await new OrderRepository(context).AddAsync(order, token, null, [], CancellationToken.None));
        await using (var context = database.CreateContext())
            Assert.NotNull(await new OrderRepository(context).AddAsync(order, token, null, [], CancellationToken.None));
        await using var read = database.CreateContext();
        var mail = Assert.Single(await read.EmailMessages.Where(message => message.Recipient == email).ToListAsync());
        Assert.Contains(order.Number, mail.Subject); Assert.Contains("2 × Saved product / Saved variant", mail.Body);
        Assert.Contains("EUR 20.00", mail.Body); Assert.Contains("Saved instructions", mail.Body);
        Assert.DoesNotContain(token.ToString(), mail.Body);
        var unavailable = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow, OrderCustomer.Create("Customer", email),
            DeliveryAddress.Create("Street 1", "1234 AB", "Utrecht", "NL"),
            [(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 1, Money.Create(1m, "EUR"))]);
        await using (var context = database.CreateContext())
            Assert.Null(await new OrderRepository(context).AddAsync(unavailable, Guid.NewGuid(), null,
                [new(unavailable.Lines[0].ProductId, unavailable.Lines[0].VariantId, 1)], CancellationToken.None));
        Assert.Equal(1, await read.EmailMessages.CountAsync(message => message.Recipient == email));
    }

    [SqlServerFact]
    public async Task Pickup_delivery_marks_sent_clears_body_and_does_not_deliver_again()
    {
        var isolated = new SqlServerDatabase();
        await isolated.InitializeAsync();
        var directory = Path.Combine(Path.GetTempPath(), "HDWebFrameworkMailTests-" + Guid.NewGuid().ToString("N"));
        try
        {

            await using (var context = isolated.CreateContext())
                await new EmailQueue(context).EnqueueAsync("customer@example.test", "HDWebFramework confirmation", "Private link", CancellationToken.None);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Email:Enabled"] = "true", ["Email:Mode"] = "Pickup", ["Email:PickupDirectory"] = directory, ["Email:From"] = "shop@example.test" }).Build();
            using var provider = MailServices(isolated, configuration);
            var worker = new EmailDeliveryWorker(provider.GetRequiredService<IServiceScopeFactory>(), configuration,
                new MailEnvironment(), NullLogger<EmailDeliveryWorker>.Instance);
            await using (var locked = isolated.CreateContext())
                await locked.EmailMessages.ExecuteUpdateAsync(update => update.SetProperty(message => message.Lease, Guid.NewGuid())
                    .SetProperty(message => message.LeaseUntil, DateTimeOffset.UtcNow.AddMinutes(5)));
            await worker.DeliverNextAsync(CancellationToken.None);
            Assert.False(Directory.Exists(directory));
            await using (var released = isolated.CreateContext())
                await released.EmailMessages.ExecuteUpdateAsync(update => update.SetProperty(message => message.LeaseUntil, DateTimeOffset.UtcNow.AddMinutes(-1)));
            await worker.DeliverNextAsync(CancellationToken.None);
            Assert.Single(Directory.GetFiles(directory));
            await using var read = isolated.CreateContext();
            var mail = await read.EmailMessages.SingleAsync();
            Assert.NotNull(mail.SentAt); Assert.Equal("", mail.Body); Assert.Equal(1, mail.Attempts);
            await worker.DeliverNextAsync(CancellationToken.None);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            await isolated.DisposeAsync();
            // This test owns a fixed-prefix, randomly named directory it created above.
            var full = Path.GetFullPath(directory);
            if (!string.Equals(Path.GetDirectoryName(full), Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(full).StartsWith("HDWebFrameworkMailTests-", StringComparison.Ordinal)
                || !Guid.TryParseExact(Path.GetFileName(full)["HDWebFrameworkMailTests-".Length..], "N", out _))
                throw new InvalidOperationException("Refusing to remove a directory not owned by this test.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }
    }

    [SqlServerFact]
    public async Task Failed_delivery_keeps_message_and_schedules_a_retry()
    {
        var isolated = new SqlServerDatabase(); await isolated.InitializeAsync();
        try
        {

            await using (var context = isolated.CreateContext())
                await new EmailQueue(context).EnqueueAsync("customer@example.test", "HDWebFramework", "Keep me", CancellationToken.None);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Enabled"] = "true", ["Email:Mode"] = "Smtp", ["Email:Smtp:Host"] = "unused.invalid", ["Email:Smtp:Port"] = "0" }).Build();
            using var provider = MailServices(isolated, configuration);
            var worker = new EmailDeliveryWorker(provider.GetRequiredService<IServiceScopeFactory>(), configuration,
                new MailEnvironment(), NullLogger<EmailDeliveryWorker>.Instance);
            await Assert.ThrowsAnyAsync<Exception>(() => worker.DeliverNextAsync(CancellationToken.None));
            await using var read = isolated.CreateContext();
            var mail = await read.EmailMessages.SingleAsync();
            Assert.Null(mail.SentAt); Assert.Equal("Keep me", mail.Body); Assert.Null(mail.LeaseUntil);
            Assert.True(mail.NextAttemptAt > DateTimeOffset.UtcNow);
            await worker.DeliverNextAsync(CancellationToken.None);
            Assert.Equal(1, (await read.EmailMessages.AsNoTracking().SingleAsync()).Attempts);
        }
        finally { await isolated.DisposeAsync(); }
    }

    [SqlServerTheory]
    [InlineData(30, 30)]
    [InlineData(5, 10)]
    public async Task Cleanup_uses_sent_time_or_creation_time_and_preserves_active_leases(int sentDays, int unsentDays)
    {
        var isolated = new SqlServerDatabase(); await isolated.InitializeAsync();
        try
        {
            var now = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
            var sentCutoff = now.AddDays(-sentDays);
            var unsentCutoff = now.AddDays(-unsentDays);
            var rows = new[] {
                StoredMail("sent-old", now.AddDays(-100), sentCutoff.AddTicks(-1)),
                StoredMail("sent-boundary", now.AddDays(-100), sentCutoff),
                StoredMail("sent-recent", now.AddDays(-100), now.AddHours(-1)),
                StoredMail("unsent-failed", unsentCutoff.AddTicks(-1), attempts: 3),
                StoredMail("unsent-never-attempted", unsentCutoff.AddDays(-1)),
                StoredMail("unsent-boundary", unsentCutoff),
                StoredMail("unsent-recent", unsentCutoff.AddTicks(1)),
                StoredMail("unsent-active", unsentCutoff.AddDays(-1), leaseUntil: now.AddMinutes(5)),
                StoredMail("sent-active", now.AddDays(-100), sentCutoff, now.AddMinutes(5)),
                StoredMail("unsent-expired-lease", unsentCutoff.AddDays(-1), leaseUntil: now),
            };
            await using (var context = isolated.CreateContext())
            { context.EmailMessages.AddRange(rows); await context.SaveChangesAsync(); }
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Email:Enabled"] = "false", ["Email:Retention:SentDays"] = sentDays.ToString(),
                ["Email:Retention:UnsentDays"] = unsentDays.ToString() }).Build();
            using var provider = MailServices(isolated, configuration);
            var worker = new EmailDeliveryWorker(provider.GetRequiredService<IServiceScopeFactory>(), configuration,
                new MailEnvironment(), NullLogger<EmailDeliveryWorker>.Instance);
            await worker.CleanupAsync(now, CancellationToken.None);
            await worker.CleanupAsync(now, CancellationToken.None);
            await using var read = isolated.CreateContext();
            Assert.Equal(new[] { "sent-active", "sent-recent", "unsent-active", "unsent-recent" },
                await read.EmailMessages.OrderBy(x => x.Subject).Select(x => x.Subject).ToArrayAsync());
            await worker.CleanupAsync(now.AddMinutes(6), CancellationToken.None);
            Assert.Equal(new[] { "sent-recent" },
                await read.EmailMessages.OrderBy(x => x.Subject).Select(x => x.Subject).ToArrayAsync());
        }
        finally { await isolated.DisposeAsync(); }
    }

    [SqlServerFact]
    public async Task Expired_unsent_messages_are_not_retried_while_waiting_for_cleanup()
    {
        var isolated = new SqlServerDatabase(); await isolated.InitializeAsync();
        try
        {
            await using (var context = isolated.CreateContext())
            {
                context.EmailMessages.Add(StoredMail("Expired", DateTimeOffset.UtcNow.AddDays(-31)));
                await context.SaveChangesAsync();
            }
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Email:Enabled"] = "true", ["Email:Mode"] = "Smtp", ["Email:Smtp:Host"] = "unused.invalid", ["Email:Smtp:Port"] = "0" }).Build();
            using var provider = MailServices(isolated, configuration);
            var worker = new EmailDeliveryWorker(provider.GetRequiredService<IServiceScopeFactory>(), configuration,
                new MailEnvironment(), NullLogger<EmailDeliveryWorker>.Instance);
            await worker.DeliverNextAsync(CancellationToken.None);
            await using var read = isolated.CreateContext();
            var mail = await read.EmailMessages.SingleAsync();
            Assert.Equal(0, mail.Attempts);
            Assert.Null(mail.SentAt);
            await worker.CleanupAsync(DateTimeOffset.UtcNow, CancellationToken.None);
            Assert.Empty(await read.EmailMessages.ToArrayAsync());
        }
        finally { await isolated.DisposeAsync(); }
    }

    [SqlServerFact]
    public async Task Starting_worker_cleans_up_even_when_email_delivery_is_disabled()
    {
        var isolated = new SqlServerDatabase(); await isolated.InitializeAsync();
        try
        {
            await using (var context = isolated.CreateContext())
            {
                context.EmailMessages.Add(StoredMail("Expired", DateTimeOffset.UtcNow.AddDays(-31)));
                await context.SaveChangesAsync();
            }
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Email:Enabled"] = "false" }).Build();
            using var provider = MailServices(isolated, configuration);
            using var worker = new EmailDeliveryWorker(provider.GetRequiredService<IServiceScopeFactory>(), configuration,
                new MailEnvironment(), NullLogger<EmailDeliveryWorker>.Instance);
            await worker.StartAsync(CancellationToken.None);
            try
            {
                await using var read = isolated.CreateContext();
                for (var attempt = 0; attempt < 50 && await read.EmailMessages.AnyAsync(); attempt++)
                    await Task.Delay(100);
                Assert.Empty(await read.EmailMessages.ToArrayAsync());
            }
            finally { await worker.StopAsync(CancellationToken.None); }
        }
        finally { await isolated.DisposeAsync(); }
    }

    private static HDWebFramework.Infrastructure.Persistence.Models.EmailMessagePersistence StoredMail(
        string subject, DateTimeOffset createdAt, DateTimeOffset? sentAt = null,
        DateTimeOffset? leaseUntil = null, int attempts = 0)
    {
        var mail = EmailQueue.Create("retention@example.test", subject, "Private message");
        mail.CreatedAt = createdAt; mail.NextAttemptAt = createdAt; mail.SentAt = sentAt;
        mail.LeaseUntil = leaseUntil; mail.Attempts = attempts;
        if (leaseUntil is not null) mail.Lease = Guid.NewGuid();
        return mail;
    }

    private static ServiceProvider MailServices(SqlServerDatabase database, IConfiguration configuration) =>
        new ServiceCollection().AddScoped(_ => database.CreateContext()).AddScoped<EmailSettingsRepository>()
            .AddSingleton(configuration).AddSingleton<IHostEnvironment>(new MailEnvironment())
            .AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider())
            .BuildServiceProvider();

    private sealed class MailEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "HDWebFramework";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
