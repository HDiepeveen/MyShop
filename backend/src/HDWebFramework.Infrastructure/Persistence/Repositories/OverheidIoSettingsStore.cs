using System.Text.Json;
using System.Security.Cryptography;
using HDWebFramework.Application.Catalog.OverheidIo;
using HDWebFramework.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace HDWebFramework.Infrastructure.Persistence.Repositories;
internal sealed class OverheidIoSettingsStore(HDWebFrameworkDbContext db, IDataProtectionProvider protection) : IOverheidIoSettingsStore
{
    private readonly IDataProtector protector = protection.CreateProtector("HDWebFramework.OverheidIo.ApiKey.v1");
    public async Task<OverheidIoSettings> GetAsync(CancellationToken ct)
    {
        var row = await db.Set<OverheidIoSettingsPersistence>().AsNoTracking().SingleAsync(ct);
        return new(row.Revision, JsonSerializer.Deserialize<string[]>(row.ColumnsJson)!, row.ProtectedApiKey is not null);
    }
    public async Task<string?> GetApiKeyAsync(CancellationToken ct)
    {
        var value = await db.Set<OverheidIoSettingsPersistence>().AsNoTracking().Select(x => x.ProtectedApiKey).SingleAsync(ct);
        if (value is null) return null;
        try { return protector.Unprotect(value); }
        catch (CryptographicException) { throw new OverheidIoConfigurationException("Vul de Overheid.io API-sleutel opnieuw in bij Voertuiginstellingen."); }
    }
    public async Task<bool> SaveAsync(SaveOverheidIoSettings settings, CancellationToken ct)
    {
        if (settings.Columns is null || settings.Columns.Any(key => !OverheidIoColumns.All.Any(c => c.Key == key)))
            throw new ArgumentException("Kies geldige voertuigvelden.");
        var key = settings.ApiKey?.Trim();
        if (key?.Length > 512 || key?.Any(char.IsControl) == true || (settings.ClearApiKey && !string.IsNullOrEmpty(key)))
            throw new ArgumentException("Controleer de API-sleutel.");
        var row = await db.Set<OverheidIoSettingsPersistence>().AsNoTracking().SingleAsync(ct);
        var encrypted = settings.ClearApiKey ? null : string.IsNullOrEmpty(key) ? row.ProtectedApiKey : protector.Protect(key);
        return await db.Set<OverheidIoSettingsPersistence>().Where(x => x.Id == 1 && x.Revision == settings.Revision)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ColumnsJson, JsonSerializer.Serialize(settings.Columns.Distinct().ToArray()))
                .SetProperty(x => x.ProtectedApiKey, encrypted).SetProperty(x => x.Revision, Guid.NewGuid()), ct) == 1;
    }
}
