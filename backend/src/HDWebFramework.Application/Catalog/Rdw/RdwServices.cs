using System.Text.Json;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Rdw;

public sealed record RdwSettings(Guid Revision, string[] Columns);
public sealed record RdwField(string Key, string Name, string Value);
public sealed record RdwVehicle(string Name, string LicensePlate, IReadOnlyList<RdwField> Fields);
public interface IRdwClient
{
    Task<string?> FindAsync(string licensePlate, CancellationToken ct);
}
public interface IRdwStore
{
    Task<RdwSettings> SettingsAsync(CancellationToken ct);
    Task<bool> SaveSettingsAsync(RdwSettings settings, CancellationToken ct);
    Task<IReadOnlyList<RdwVehicle>?> PublicVehiclesAsync(Guid productId, CancellationToken ct);
}
public static class RdwDisplay
{
    public static IReadOnlyList<RdwField> Fields(string json, IReadOnlyCollection<string> selected)
    {
        using var doc = JsonDocument.Parse(json);
        var fields = new List<RdwField>();
        foreach (var column in RdwColumns.All.Where(c => selected.Contains(c.Key)))
        {
            if (!doc.RootElement.TryGetProperty(column.Key, out var raw) || raw.ValueKind == JsonValueKind.Null)
                continue;
            var value = raw.ValueKind == JsonValueKind.String ? raw.GetString()! : raw.ToString();
            if (string.IsNullOrWhiteSpace(value)) continue;
            if ((column.Key.StartsWith("datum_") || column.Key.Contains("vervaldatum") ||
                column.Key.StartsWith("registratie_datum")) && value.Length == 8 &&
                DateOnly.TryParseExact(value, "yyyyMMdd", out var date))
                value = date.ToString("dd-MM-yyyy");
            else if (column.Type == "calendar_date" && DateTime.TryParse(value,
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var timestamp))
                value = timestamp.ToString("dd-MM-yyyy");
            fields.Add(new(column.Key, column.Name, value));
        }
        return fields;
    }
}
