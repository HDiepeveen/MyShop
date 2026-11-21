namespace HDWebFramework.Application.Catalog.OverheidIo;

public sealed record OverheidIoSettings(Guid Revision, string[] Columns, bool HasApiKey);
public sealed record SaveOverheidIoSettings(Guid Revision, string[] Columns, string? ApiKey, bool ClearApiKey = false);
public interface IOverheidIoSettingsStore
{
    Task<OverheidIoSettings> GetAsync(CancellationToken ct);
    Task<bool> SaveAsync(SaveOverheidIoSettings settings, CancellationToken ct);
    Task<string?> GetApiKeyAsync(CancellationToken ct);
}
public interface IOverheidIoClient
{
    Task<string?> FindAsync(string licensePlate, CancellationToken ct);
}
public sealed class OverheidIoConfigurationException(string message) : Exception(message);
