using System.Net;
using System.Text.Json;
using HDWebFramework.Infrastructure.Rdw;

namespace HDWebFramework.Infrastructure.Tests.Rdw;

public sealed class RdwClientTests
{
    [Theory]
    [InlineData("[{\"brandstof_omschrijving\":\"Benzine\"},{\"brandstof_omschrijving\":\"LPG\"},{\"brandstof_omschrijving\":\"Benzine\"}]", "Benzine / LPG")]
    [InlineData("[]", null)]
    public async Task CombinesFuelTypesWithoutDuplicates(string fuels, string? expected)
    {
        var handler = new Handler("[{\"kenteken\":\"AB12CD\",\"merk\":\"OPEL\"}]", fuels);
        var result = await new RdwClient(new HttpClient(handler)).FindAsync("AB12CD", default);
        using var json = JsonDocument.Parse(result!);
        Assert.Equal("OPEL", json.RootElement.GetProperty("merk").GetString());
        Assert.Equal(expected, json.RootElement.TryGetProperty("brandstof_omschrijving", out var fuel) ? fuel.GetString() : null);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("8ys7-d773.json?kenteken=AB12CD", handler.Requests[1]);
    }

    [Fact]
    public async Task UnknownPlateDoesNotQueryFuel()
    {
        var handler = new Handler("[]", "[]");
        Assert.Null(await new RdwClient(new HttpClient(handler)).FindAsync("AB12CD", default));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task PreservesConsumptionPerFuelAndElectricFieldsWithoutEmptyValues()
    {
        var fuels = """[{"brandstof_omschrijving":"Benzine","brandstofverbruik_gecombineerd":"6.2","co2_uitstoot_gecombineerd":"140","geluidsniveau_stationair":""},{"brandstof_omschrijving":"LPG","brandstofverbruik_gecombineerd":"8.1","co2_uitstoot_gecombineerd":"130","geluidsniveau_stationair":null}]""";
        var result = await new RdwClient(new HttpClient(new Handler("""[{"kenteken":"AB12CD"}]""", fuels)))
            .FindAsync("AB12CD", default);
        using var json = JsonDocument.Parse(result!);
        Assert.Equal("Benzine: 6.2 / LPG: 8.1", json.RootElement.GetProperty("brandstofverbruik_gecombineerd").GetString());
        Assert.Equal("Benzine: 140 / LPG: 130", json.RootElement.GetProperty("co2_uitstoot_gecombineerd").GetString());
        Assert.False(json.RootElement.TryGetProperty("geluidsniveau_stationair", out _));

        var electric = """[{"brandstof_omschrijving":"Elektriciteit","elektrisch_verbruik_enkel_elektrisch_wltp":"135.00","actie_radius_enkel_elektrisch_wltp":"450"}]""";
        result = await new RdwClient(new HttpClient(new Handler("""[{"kenteken":"AB12CD"}]""", electric)))
            .FindAsync("AB12CD", default);
        using var ev = JsonDocument.Parse(result!);
        Assert.Equal("135.00", ev.RootElement.GetProperty("elektrisch_verbruik_enkel_elektrisch_wltp").GetString());
        Assert.Equal("450", ev.RootElement.GetProperty("actie_radius_enkel_elektrisch_wltp").GetString());
    }

    private sealed class Handler(string vehicle, string fuel) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(Requests.Count == 1 ? vehicle : fuel, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
