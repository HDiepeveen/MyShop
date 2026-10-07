using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Notifications;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

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
        var directory = Path.Combine(Path.GetTempPath(), "MyShopMailTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var services = new ServiceCollection().AddScoped(_ => isolated.CreateContext());
            using var provider = services.BuildServiceProvider();
            await using (var context = isolated.CreateContext())
                await new EmailQueue(context).EnqueueAsync("customer@example.test", "MyShop confirmation", "Private link", CancellationToken.None);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Email:Mode"] = "Pickup", ["Email:PickupDirectory"] = directory, ["Email:From"] = "shop@example.test" }).Build();
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
                || !Path.GetFileName(full).StartsWith("MyShopMailTests-", StringComparison.Ordinal)
                || !Guid.TryParseExact(Path.GetFileName(full)["MyShopMailTests-".Length..], "N", out _))
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
            using var provider = new ServiceCollection().AddScoped(_ => isolated.CreateContext()).BuildServiceProvider();
            await using (var context = isolated.CreateContext())
                await new EmailQueue(context).EnqueueAsync("customer@example.test", "MyShop", "Keep me", CancellationToken.None);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Mode"] = "Smtp", ["Email:Smtp:Host"] = "unused.invalid", ["Email:Smtp:Port"] = "0" }).Build();
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

    private sealed class MailEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "MyShop";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
