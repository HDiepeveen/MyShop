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
        var descriptions = (fuels ?? []).Select(f => f.TryGetProperty("brandstof_omschrijving", out var value)
                ? value.GetString() : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var vehicle = JsonNode.Parse(records[0].GetRawText())!.AsObject();
        if (descriptions.Length > 0)
            vehicle["brandstof_omschrijving"] = string.Join(" / ", descriptions);
        return vehicle.ToJsonString();
    }
}
