using System.Net.Http.Json;
using System.Text.Json;
using HDWebFramework.Application.Catalog.Rdw;

namespace HDWebFramework.Infrastructure.Rdw;

internal sealed class RdwClient(HttpClient http) : IRdwClient
{
    public async Task<string?> FindAsync(string licensePlate, CancellationToken ct)
    {
        var records = await http.GetFromJsonAsync<JsonElement[]>(
            "https://opendata.rdw.nl/resource/m9d7-ebf2.json?kenteken=" +
            Uri.EscapeDataString(licensePlate) + "&$limit=1", ct);
        return records is { Length: > 0 } ? records[0].GetRawText() : null;
    }
}
