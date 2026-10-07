using System.Net.Mail;
using MyShop.Application.Notifications;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Notifications;

internal sealed class EmailQueue(MyShopDbContext context) : IEmailQueue
{
    public async Task EnqueueAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var message = Create(recipient, subject, body);
        context.EmailMessages.Add(message);
        await context.SaveChangesAsync(cancellationToken);
    }

    internal static EmailMessagePersistence Create(string recipient, string subject, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        if (recipient.Length > 320 || subject.Length > 200 || recipient.Any(char.IsControl) || subject.Any(char.IsControl))
            throw new ArgumentException("Invalid email headers.");
        var address = new MailAddress(recipient);
        if (!string.Equals(address.Address, recipient, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Email must contain a single address.");
        var now = DateTimeOffset.UtcNow;
        return new() { Id = Guid.NewGuid(), Recipient = recipient, Subject = subject, Body = body, CreatedAt = now, NextAttemptAt = now };
    }
}
