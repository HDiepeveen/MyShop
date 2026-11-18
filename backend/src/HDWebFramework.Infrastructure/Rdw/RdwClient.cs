using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using HDWebFramework.Application.Catalog.Rdw;

namespace HDWebFramework.Infrastructure.Rdw;

internal sealed class RdwClient(HttpClient http) : IRdwClient
{
    public async Task<string?> FindAsync(string licensePlate, CancellationToken ct)
    {
        var query = "?kenteken=" + Uri.EscapeDataString(licensePlate);
        var records = await http.GetFromJsonAsync<JsonElement[]>(
            "https://opendata.rdw.nl/resource/m9d7-ebf2.json" + query + "&$limit=1", ct);
        if (records is not { Length: > 0 }) return null;

        var fuels = await http.GetFromJsonAsync<JsonElement[]>(
            "https://opendata.rdw.nl/resource/8ys7-d773.json" + query + "&$limit=100", ct);
        var vehicle = JsonNode.Parse(records[0].GetRawText())!.AsObject();
        var fuelRecords = fuels ?? [];
        var fuelColumns = fuelRecords.SelectMany(f => f.EnumerateObject().Select(p => p.Name))
            .Where(key => key != "kenteken").Distinct();
        foreach (var key in fuelColumns)
        {
            var values = fuelRecords.Select(f =>
            {
                if (!f.TryGetProperty(key, out var raw) || raw.ValueKind == JsonValueKind.Null) return null;
                var value = raw.ValueKind == JsonValueKind.String ? raw.GetString() : raw.ToString();
                if (string.IsNullOrWhiteSpace(value)) return null;
                if (fuelRecords.Length > 1 && key != "brandstof_omschrijving" &&
                    f.TryGetProperty("brandstof_omschrijving", out var fuel) && !string.IsNullOrWhiteSpace(fuel.GetString()))
                    return fuel.GetString() + ": " + value;
                return value;
            }).Where(value => value is not null).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (values.Length > 0) vehicle[key] = string.Join(" / ", values);
        }
        return vehicle.ToJsonString();
    }
}
