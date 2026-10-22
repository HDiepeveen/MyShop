using System.Text.RegularExpressions;
namespace HDWebFramework.Application.Checkout.MollieSettings;
public sealed record MollieSettingsSnapshot(string ReturnUrl, string WebhookUrl, bool KeyConfigured, bool Managed, Guid Revision);
public sealed record UpdateMollieSettingsCommand(string ReturnUrl, string WebhookUrl, string? ApiKey, bool ClearKey, Guid Revision);
public sealed record MollieConnectionCheck(bool Configured, bool KeyAccepted, bool IdealAvailable);
public interface IMollieSettingsStore
{
    Task<MollieSettingsSnapshot> GetAsync(CancellationToken ct);
    Task<MollieSettingsSnapshot?> SaveAsync(UpdateMollieSettingsCommand value, CancellationToken ct);
    Task<MollieConnectionCheck?> CheckAsync(Guid revision, CancellationToken ct);
}
public sealed class ManageMollieSettings(IMollieSettingsStore store)
{
    public Task<MollieSettingsSnapshot> GetAsync(CancellationToken ct) => store.GetAsync(ct);
    public Task<MollieConnectionCheck?> CheckAsync(Guid revision, CancellationToken ct) => store.CheckAsync(revision, ct);
    public Task<MollieSettingsSnapshot?> SaveAsync(UpdateMollieSettingsCommand value, bool development, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(value.ApiKey) ? null : value.ApiKey.Trim();
        if (key is not null && !Regex.IsMatch(key, "^test_[A-Za-z0-9]{20,64}$"))
            throw new ArgumentException("Gebruik uitsluitend een Mollie testsleutel die begint met test_.");
        if (key is not null && value.ClearKey) throw new ArgumentException("Kies vervangen of wissen.");
        var returnUrl = Url(value.ReturnUrl, development, "/winkel/betaling");
        var webhookUrl = Url(value.WebhookUrl, false, "/api/payments/mollie/webhook");
        return store.SaveAsync(value with { ApiKey = key, ReturnUrl = returnUrl, WebhookUrl = webhookUrl }, ct);
    }
    private static string Url(string? value, bool allowLocal, string path)
    {
        value = value?.Trim() ?? "";
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || value.Length > 2000 || value.Any(char.IsControl)
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.AbsolutePath != path
            || (uri.IsLoopback ? !allowLocal || uri.Scheme is not ("http" or "https") : uri.Scheme != "https"))
            throw new ArgumentException("Gebruik een HTTPS-adres met het juiste pad, zonder query of fragment. Alleen het terugkeeradres mag lokaal zijn in ontwikkeling.");
        return uri.AbsoluteUri;
    }
}
