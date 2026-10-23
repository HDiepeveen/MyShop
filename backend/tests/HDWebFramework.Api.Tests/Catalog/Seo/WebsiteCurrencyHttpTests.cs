using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HDWebFramework.Api.Tests.Security;

namespace HDWebFramework.Api.Tests.Catalog.Seo;

public sealed class WebsiteCurrencyHttpTests
{
    [SecuritySqlFact]
    public async Task CurrencyPersistsAndInvalidInputDoesNotChangeIt()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); await host.Login(); await host.Csrf();
        var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal("EUR", settings["currency"]!.GetValue<string>());
        settings["currency"] = " srd ";
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var stored = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal("SRD", stored["currency"]!.GetValue<string>());
        var publicSettings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!;
        Assert.Equal("SRD", publicSettings["currency"]!.GetValue<string>());
        foreach (var invalid in new[] { "", "EU", "123", "EURO" })
        {
            stored["currency"] = invalid;
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings", stored)).StatusCode);
        }
        var unchanged = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal("SRD", unchanged["currency"]!.GetValue<string>());
        Assert.Equal(stored["revision"]!.GetValue<string>(), unchanged["revision"]!.GetValue<string>());
    }
}
