using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using HDWebFramework.Application.Catalog.Rdw;

namespace HDWebFramework.Infrastructure.Rdw;

internal sealed class RdwClient(HttpClient http) : IRdwClient
{
    public async Task<string?> FindAsync(string licensePlate, CancellationToken ct)
    {
        try
        {
            return await FindAtAsync("https://opendata.rdw.nl", licensePlate, ct);
        }
        catch (HttpRequestException ex) when (ex.HttpRequestError is HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError)
        {
            // The same RDW datasets are also served on RDW's Socrata tenant.
            // Only connection failures use this fixed fallback; HTTP rejections are not retried.
            return await FindAtAsync("https://rdw.azure-westeurope-prod.socrata.com", licensePlate, ct);
        }
    }

    private async Task<string?> FindAtAsync(string origin, string licensePlate, CancellationToken ct)
    {
        var query = "?kenteken=" + Uri.EscapeDataString(licensePlate);
        var records = await http.GetFromJsonAsync<JsonElement[]>(
            origin + "/resource/m9d7-ebf2.json" + query + "&$limit=1", ct);
        if (records is not { Length: > 0 }) return null;

        var fuels = await http.GetFromJsonAsync<JsonElement[]>(
            origin + "/resource/8ys7-d773.json" + query + "&$limit=100", ct);
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
