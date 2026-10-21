using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Seo;

public sealed class HeaderTextHttpTests
{
    [SecuritySqlFact]
    public async Task Header_text_can_be_changed_without_removing_company_contact_details()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); await host.Login(); await host.Csrf();
        foreach (var text in new[] { " Vandaag geopend ", "", "Andere tekst" })
        {
            var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
            settings["company"]!["phone"] = "038 1234567";
            settings["headerText"] = text;
            settings["showHeaderPhone"] = false; settings["showHeaderWhatsapp"] = true;
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
            var stored = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
            Assert.Equal(string.IsNullOrWhiteSpace(text) ? null : text.Trim(), stored["headerText"]?.GetValue<string>());
            var publicSettings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!;
            Assert.Equal(string.IsNullOrWhiteSpace(text) ? null : text.Trim(), publicSettings["headerText"]?.GetValue<string>());
            Assert.Equal("038 1234567", publicSettings["contactPhone"]!.GetValue<string>());
            Assert.False(publicSettings["showHeaderPhone"]!.GetValue<bool>()); Assert.True(publicSettings["showHeaderWhatsapp"]!.GetValue<bool>());
            Assert.False(stored["showHeaderPhone"]!.GetValue<bool>());
            var company = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/company"))!;
            Assert.Equal("038 1234567", company["phone"]!.GetValue<string>());
        }
    }
}
