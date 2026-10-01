using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Dashboard;

public sealed class DashboardHttpTests
{
    [SecuritySqlFact]
    public async Task DashboardRequiresAdministratorAndReturnsEmptySnapshot()
    {
        await using var anonymous = await SecurityHost.Create();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.Client.GetAsync("/api/dashboard")).StatusCode);

        await using var customer = await SecurityHost.Create();
        await customer.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await customer.Client.PostAsJsonAsync(
            "/api/customer/auth/register", new { email = "customer@example.test", password = SecurityHost.Password }))
            .StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.Client.GetAsync("/api/dashboard")).StatusCode);

        await using var admin = await SecurityHost.Create();
        await admin.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.Login()).StatusCode);
        var response = await admin.Client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("productCount").GetInt32());
        Assert.Equal(0, body.GetProperty("publishedProductCount").GetInt32());
        Assert.Equal(0, body.GetProperty("draftProductCount").GetInt32());
        Assert.Equal(0, body.GetProperty("customerCount").GetInt32());
        Assert.Empty(body.GetProperty("orders").EnumerateArray());
        Assert.Empty(body.GetProperty("activeRevenue").EnumerateArray());
        Assert.Empty(body.GetProperty("lowStock").EnumerateArray());
        Assert.Empty(body.GetProperty("recentOrders").EnumerateArray());
    }
}
