using System.Net;
using System.Text.Json;
using HDWebFramework.Application.Catalog.OverheidIo;
using HDWebFramework.Infrastructure.Rdw;
namespace HDWebFramework.Infrastructure.Tests.Rdw;
public sealed class OverheidIoClientTests
{
    [Fact]
    public async Task UsesExactEndpointAndPrivateHeaderAndFlattensNestedFuelAndAxles()
    {
        var handler = new Handler(HttpStatusCode.OK);
        var client = new OverheidIoClient(new HttpClient(handler), new Settings("test-key"));
        var json = await client.FindAsync("AB12CD", default);
        Assert.Equal("https://api.overheid.io/voertuiggegevens/AB12CD", handler.Url);
        Assert.Equal("test-key", handler.Key);
        using var doc = JsonDocument.Parse(json!);
        Assert.Equal("Benzine / LPG", doc.RootElement.GetProperty("brandstof.brandstof_omschrijving").GetString());
        Assert.Equal("Benzine: 6.2 / LPG: 8", doc.RootElement.GetProperty("brandstof.brandstofverbruik_gecombineerd").GetString());
        Assert.Equal("As 1: 150 / As 2: 160", doc.RootElement.GetProperty("as.spoorbreedte").GetString());
        Assert.Equal("Ja", doc.RootElement.GetProperty("wam_verzekerd").GetString());
        Assert.False(doc.RootElement.TryGetProperty("_links", out _));
        Assert.DoesNotContain("test-key", json);
    }
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ReportsCredentialsAndLimitsWithoutDisclosingKey(HttpStatusCode status)
    {
        var client = new OverheidIoClient(new HttpClient(new Handler(status)), new Settings("private-test-key"));
        var ex = await Assert.ThrowsAsync<OverheidIoConfigurationException>(() => client.FindAsync("AB12CD", default));
        Assert.DoesNotContain("private-test-key", ex.Message);
    }
    [Fact]
    public async Task MissingKeyDoesNotSendARequestAndMissingVehicleReturnsNull()
    {
        var handler = new Handler(HttpStatusCode.NotFound);
        await Assert.ThrowsAsync<OverheidIoConfigurationException>(() => new OverheidIoClient(new HttpClient(handler), new Settings(null)).FindAsync("AB12CD", default));
        Assert.Null(handler.Url);
        Assert.Null(await new OverheidIoClient(new HttpClient(handler), new Settings("test-key")).FindAsync("AB12CD", default));
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TriesSuppliedPlateAndOnlyTriesWithoutDashesAfterNotFound(bool formattedExists)
    {
        var handler = new PlateHandler(formattedExists);
        var result = await new OverheidIoClient(new HttpClient(handler), new Settings("test-key")).FindAsync("28-zgb-8", default);
        Assert.NotNull(result);
        Assert.Equal(formattedExists ? new[] { "/voertuiggegevens/28-ZGB-8" }
            : new[] { "/voertuiggegevens/28-ZGB-8", "/voertuiggegevens/28ZGB8" }, handler.Paths);
    }
    private sealed class PlateHandler(bool formattedExists) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(!formattedExists && Paths.Count == 1 ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            { Content = new StringContent("""{"kenteken":"28ZGB8","merk":"OPEL"}""") });
        }
    }
    private sealed class Settings(string? key) : IOverheidIoSettingsStore
    {
        public Task<string?> GetApiKeyAsync(CancellationToken ct) => Task.FromResult(key);
        public Task<OverheidIoSettings> GetAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> SaveAsync(SaveOverheidIoSettings value, CancellationToken ct) => throw new NotImplementedException();
    }
    private sealed class Handler(HttpStatusCode status) : HttpMessageHandler
    {
        public string? Url, Key;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Url = request.RequestUri!.ToString(); Key = request.Headers.GetValues("ovio-api-key").Single();
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("""{"kenteken":"AB12CD","wam_verzekerd":true,"brandstof":[{"brandstof_omschrijving":"Benzine","brandstofverbruik_gecombineerd":6.2},{"brandstof_omschrijving":"LPG","brandstofverbruik_gecombineerd":8}],"as":[{"as_nummer":1,"spoorbreedte":150},{"as_nummer":2,"spoorbreedte":160}],"_links":{"self":{"href":"/voertuiggegevens/AB12CD"}}}""") });
        }
    }
}
