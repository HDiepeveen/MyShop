using System.Net.Mail;

namespace MyShop.Application.Notifications;

public sealed record EmailSettingsSnapshot(bool Enabled, string Host, int Port, string UserName,
    string FromAddress, string FromName, string PublicBaseUrl, bool PasswordConfigured, Guid Revision);
public sealed record UpdateEmailSettingsCommand(bool Enabled, string Host, int Port, string UserName,
    string FromAddress, string FromName, string PublicBaseUrl, string? Password, bool ClearPassword, Guid Revision);
public interface IEmailSettingsRepository
{
    Task<EmailSettingsSnapshot> GetAsync(CancellationToken cancellationToken);
    Task<EmailSettingsSnapshot?> SaveAsync(UpdateEmailSettingsCommand command, CancellationToken cancellationToken);
}

public sealed class GetEmailSettings(IEmailSettingsRepository repository)
{
    public Task<EmailSettingsSnapshot> ExecuteAsync(CancellationToken cancellationToken) => repository.GetAsync(cancellationToken);
}

public sealed class UpdateEmailSettings(IEmailSettingsRepository repository)
{
    public async Task<EmailSettingsSnapshot?> ExecuteAsync(UpdateEmailSettingsCommand command, bool development, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Revision == Guid.Empty) throw new ArgumentException("Vernieuw eerst de instellingen.");
        command = command with { Host = Text(command.Host, 253), UserName = Text(command.UserName, 320),
            FromAddress = Text(command.FromAddress, 320), FromName = Text(command.FromName, 200), PublicBaseUrl = Text(command.PublicBaseUrl, 2000) };
        if (command.Port is < 1 or > 65535 || command.Port == 465)
            throw new ArgumentException("Gebruik een SMTP-poort met STARTTLS, meestal 587. Poort 465 wordt niet ondersteund.");
        if (command.Host.Length > 0 && Uri.CheckHostName(command.Host) == UriHostNameType.Unknown)
            throw new ArgumentException("Vul een servernaam of IP-adres in, zonder protocol of pad.");
        if (command.FromAddress.Length > 0) Address(command.FromAddress);
        if (command.Password?.Length > 1024 || command.Password?.Contains('\0') == true
            || (command.ClearPassword && !string.IsNullOrEmpty(command.Password)))
            throw new ArgumentException("Geef één wachtwoordkeuze op: behouden, vervangen of wissen.");
        if (command.PublicBaseUrl.Length == 0) throw new ArgumentException("Vul de winkel-URL voor accountlinks in.");
        if (!Uri.TryCreate(command.PublicBaseUrl, UriKind.Absolute, out var uri)
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0
            || (uri.Scheme != "https" && !(development && uri.Scheme == "http" && uri.IsLoopback)))
            throw new ArgumentException("Gebruik een HTTPS-winkel-URL zonder query of fragment. Lokale HTTP is alleen voor ontwikkeling.");
        if (command.Enabled && (command.Host.Length == 0 || command.FromAddress.Length == 0 || command.PublicBaseUrl.Length == 0))
            throw new ArgumentException("Vul server, afzenderadres en winkel-URL in voordat je verzending inschakelt.");
        return await repository.SaveAsync(command, cancellationToken);
    }
    private static string Text(string? value, int maximum)
    {
        value = value?.Trim() ?? "";
        if (value.Length > maximum || value.Any(char.IsControl)) throw new ArgumentException("Een veld bevat te veel tekens of ongeldige tekens.");
        return value;
    }
    internal static void Address(string email)
    {
        try
        {
            if (email.Length > 320 || email.Any(char.IsControl) || new MailAddress(email).Address != email) throw new FormatException();
        }
        catch (FormatException) { throw new ArgumentException("Vul een geldig e-mailadres in."); }
    }
}

public sealed class SendEmailTest(IEmailSettingsRepository repository, IEmailQueue queue)
{
    public async Task<bool> ExecuteAsync(string recipient, Guid revision, CancellationToken cancellationToken)
    {
        recipient = recipient?.Trim() ?? "";
        if (string.IsNullOrEmpty(recipient)) throw new ArgumentException("Vul een testontvanger in.");
        UpdateEmailSettings.Address(recipient);
        var settings = await repository.GetAsync(cancellationToken);
        if (!settings.Enabled || settings.Revision != revision) return false;
        await queue.EnqueueAsync(recipient, "MyShop testmail", "Deze testmail is verzonden met de opgeslagen MyShop e-mailinstellingen.", cancellationToken);
        return true;
    }
}
