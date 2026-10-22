using HDWebFramework.Infrastructure.Security;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HDWebFramework.Application.Checkout.MollieSettings;
using HDWebFramework.Infrastructure.Persistence;
using HDWebFramework.Infrastructure.Persistence.Configurations;
namespace HDWebFramework.Infrastructure.Payments;
internal sealed class MollieSettingsStore(HDWebFrameworkDbContext context, IConfiguration configuration, IDataProtectionProvider protection, IHttpClientFactory http, IHostEnvironment environment)
    : IMollieSettingsStore
{
    private readonly IDataProtector protector = protection.CreateProtector("HDWebFramework.Mollie.TestApiKey.v1");
    internal async Task<(MollieSettingsSnapshot Public, MolliePaymentConfiguration Runtime)> RuntimeAsync(CancellationToken ct)
    {
        var row = await context.PaymentOptions.AsNoTracking().SingleAsync(x => x.Id == PaymentOptionsPersistenceConfiguration.SingletonId, ct);
        if (!row.MollieManaged)
        {
            var fallback = new MolliePaymentConfiguration(configuration);
            return (new(configuration["Payments:Mollie:ReturnUrl"] ?? "", configuration["Payments:Mollie:WebhookUrl"] ?? "",
                fallback.ApiKey is not null, false, row.MollieRevision), fallback);
        }
        string? key = null;
        try { if (row.ProtectedMollieKey is not null) key = await ReadKeyAsync(row.ProtectedMollieKey, ct); }
        catch (Exception ex) when (ex is CryptographicException or FormatException) { }
        var runtime = new MolliePaymentConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Payments:Mollie:ApiKey"] = key, ["Payments:Mollie:ReturnUrl"] = row.MollieReturnUrl, ["Payments:Mollie:WebhookUrl"] = row.MollieWebhookUrl
        }).Build());
        return (new(row.MollieReturnUrl ?? "", row.MollieWebhookUrl ?? "", runtime.ApiKey is not null, true, row.MollieRevision), runtime);
    }
    internal async Task UpgradeSecretAsync(CancellationToken ct)
    {
        var encrypted = await context.PaymentOptions.Select(x => x.ProtectedMollieKey).SingleAsync(ct);
        if (encrypted is not null) await ReadKeyAsync(encrypted, ct);
    }
    private async Task<string> ReadKeyAsync(string encrypted, CancellationToken ct)
    {
        var result = FrameworkSecretProtection.Read(protection, "HDWebFramework.Mollie.TestApiKey.v1", encrypted, configuration, environment);
        if (result.Replacement is not null)
            await context.PaymentOptions.Where(x => x.ProtectedMollieKey == encrypted)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.ProtectedMollieKey, result.Replacement), ct);
        return result.Value;
    }
    public async Task<MollieSettingsSnapshot> GetAsync(CancellationToken ct) => (await RuntimeAsync(ct)).Public;
    public async Task<MollieSettingsSnapshot?> SaveAsync(UpdateMollieSettingsCommand value, CancellationToken ct)
    {
        var current = await context.PaymentOptions.AsNoTracking().SingleAsync(x => x.Id == PaymentOptionsPersistenceConfiguration.SingletonId, ct);
        string? protectedKey = value.ClearKey ? null : value.ApiKey is not null ? protector.Protect(value.ApiKey)
            : current.MollieManaged ? current.ProtectedMollieKey is null ? null : protector.Protect(await ReadKeyAsync(current.ProtectedMollieKey, ct))
            : new MolliePaymentConfiguration(configuration).ApiKey is string fallback ? protector.Protect(fallback) : null;
        if (current.CheckoutEnabled && current.OnlinePaymentEnabled && !current.PayLaterEnabled && protectedKey is null)
            throw new ArgumentException("Schakel eerst later betalen in voordat je de laatste online betaalmogelijkheid verwijdert.");
        var revision = Guid.NewGuid();
        var changed = await context.PaymentOptions.Where(x => x.Id == current.Id && x.MollieRevision == value.Revision && x.Version == current.Version)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.MollieManaged, true).SetProperty(x => x.ProtectedMollieKey, protectedKey)
                .SetProperty(x => x.MollieReturnUrl, value.ReturnUrl).SetProperty(x => x.MollieWebhookUrl, value.WebhookUrl)
                .SetProperty(x => x.MollieRevision, revision)
                .SetProperty(x => x.OnlinePaymentEnabled, x => protectedKey == null ? false : x.OnlinePaymentEnabled)
                .SetProperty(x => x.Version, Guid.NewGuid()), ct);
        return changed == 0 ? null : await GetAsync(ct);
    }
    public async Task<MollieConnectionCheck?> CheckAsync(Guid revision, CancellationToken ct)
    {
        var current = await RuntimeAsync(ct);
        if (current.Public.Revision != revision) return null;
        if (!current.Runtime.IsConfigured) return new(false, false, false);
        MollieConnectionCheck result;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mollie.com/v2/methods");
            request.Headers.Authorization = new("Bearer", current.Runtime.ApiKey);
            using var response = await http.CreateClient("Mollie").SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) result = new(true, false, false);
            else
            {
                using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                var ideal = json.RootElement.GetProperty("_embedded").GetProperty("methods").EnumerateArray().Any(x => x.GetProperty("id").GetString() == "ideal");
                result = new(true, true, ideal);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException || ex is OperationCanceledException && !ct.IsCancellationRequested)
        { result = new(true, false, false); }
        return (await GetAsync(ct)).Revision == revision ? result : null;
    }
}
