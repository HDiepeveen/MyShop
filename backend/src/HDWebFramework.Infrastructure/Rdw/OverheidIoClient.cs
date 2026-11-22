using HDWebFramework.Domain.Catalog;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using HDWebFramework.Application.Catalog.OverheidIo;
namespace HDWebFramework.Infrastructure.Rdw;
internal sealed class OverheidIoClient(HttpClient http, IOverheidIoSettingsStore settings) : IOverheidIoClient
{
    public async Task<string?> FindAsync(string licensePlate, CancellationToken ct)
    {
        var apiKey = await settings.GetApiKeyAsync(ct);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new OverheidIoConfigurationException("Vul eerst de Overheid.io API-sleutel in bij Voertuiginstellingen.");
        var normalized = OverheidIoAttributeValue.NormalizePlate(licensePlate);
        var supplied = licensePlate.Trim().Replace(" ", "").ToUpperInvariant();
        var result = await LookupAsync(supplied, apiKey, ct);
        return result is null && supplied != normalized ? await LookupAsync(normalized, apiKey, ct) : result;
    }

    private async Task<string?> LookupAsync(string licensePlate, string apiKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.overheid.io/voertuiggegevens/" + Uri.EscapeDataString(licensePlate));
        request.Headers.Add("ovio-api-key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new OverheidIoConfigurationException("Overheid.io weigert de API-sleutel. Controleer de sleutel en de toegang tot voertuiggegevens.");
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new OverheidIoConfigurationException("De opvraaglimiet van Overheid.io is bereikt. Probeer later opnieuw.");
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("kenteken", out var plate) || plate.ValueKind != JsonValueKind.String)
            throw new JsonException("Overheid.io geeft geen voertuigobject terug.");
        var result = new JsonObject();
        Flatten(doc.RootElement, "", "", result);
        return result.ToJsonString();
    }
    private static void Flatten(JsonElement element, string prefix, string label, JsonObject result)
    {
        foreach (var property in element.EnumerateObject().Where(p => !p.Name.StartsWith('_')))
        {
            var key = prefix + property.Name;
            var value = property.Value;
            if (value.ValueKind == JsonValueKind.Object) { Flatten(value, key + ".", label, result); continue; }
            if (value.ValueKind == JsonValueKind.Array)
            {
                var records = value.EnumerateArray().ToArray();
                for (var index = 0; index < records.Length; index++)
                {
                    var item = records[index];
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var itemLabel = "";
                    if (records.Length > 1)
                    {
                        itemLabel = item.TryGetProperty("brandstof_omschrijving", out var fuel) ? fuel.ToString()
                            : property.Name == "as" && item.TryGetProperty("as_nummer", out var axle) ? "As " + axle.ToString()
                            : property.Name + " " + (index + 1);
                    }
                    Flatten(item, key + ".", itemLabel, result);
                }
                continue;
            }
            var text = value.ValueKind switch { JsonValueKind.True => "Ja", JsonValueKind.False => "Nee",
                JsonValueKind.Null => "", JsonValueKind.String => value.GetString()!, _ => value.ToString() };
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (label.Length > 0 && property.Name != "brandstof_omschrijving") text = label + ": " + text;
            result[key] = result[key] is null ? text : result[key]!.GetValue<string>() + " / " + text;
        }
    }
}
