namespace HDWebFramework.Application.Notifications;

public interface IEmailQueue
{
    Task EnqueueAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}
