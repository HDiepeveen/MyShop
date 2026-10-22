using System.Net.Mail;
using HDWebFramework.Application.Notifications;
using HDWebFramework.Infrastructure.Persistence;
using HDWebFramework.Infrastructure.Persistence.Models;

namespace HDWebFramework.Infrastructure.Notifications;

internal sealed class EmailQueue(HDWebFrameworkDbContext context) : IEmailQueue
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
