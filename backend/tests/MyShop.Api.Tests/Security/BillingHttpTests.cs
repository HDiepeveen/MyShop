using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
namespace MyShop.Api.Tests.Security;
public sealed class BillingHttpTests
{
    [SecuritySqlFact]
    public async Task Company_and_percentage_management_require_administrator_and_revision()
    {
        await using var host = await SecurityHost.Create(); await host.Csrf();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/billing/company")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode); await host.Csrf();
        var company = await host.Client.GetFromJsonAsync<JsonElement>("/api/billing/company");
        var data = new { name = "Test seller", addressLine = "Street 1", postalCode = "1234 AB", city = "Utrecht", vatId = "NL123456789B01", kvkNumber = "12345678", invoicePrefix = "INV-", revision = company.GetProperty("revision").GetGuid() };
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/billing/company", data)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/billing/company", data)).StatusCode);
        var saved = await host.Client.PostAsJsonAsync("/api/billing/vat-rates", new { id = (Guid?)null, name = "Custom", percentage = 12.5, exempt = false, enabled = true, revision = (Guid?)null });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/billing/vat-rates", new { name = "Invalid", percentage = 12.555, exempt = false, enabled = true })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "billing-client@example.test", password = SecurityHost.Password })).StatusCode); await host.Csrf();
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/billing/company")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/billing/vat-rates")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/customer/orders/" + Guid.NewGuid() + "/invoice")).StatusCode);
    }
}
