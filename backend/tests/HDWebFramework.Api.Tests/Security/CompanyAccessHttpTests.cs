using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Application.Security;

namespace HDWebFramework.Api.Tests.Security;

public sealed class CompanyAccessHttpTests
{
    [SecuritySqlFact]
    public async Task AllAssignableSectionsWorkAndRevokedWritePermissionsStopImmediately()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); await host.Login(); await host.Csrf();
        var role = await CreateRole(host.Client, "All sections", CompanyPermissions.Items.Keys.ToArray());
        var employee = await CreateAccount(host.Client, "all-sections-user", [role.Id]);
        var cookies = new CookieContainer();
        using var client = new HttpClient(new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = false }) { BaseAddress = host.Client.BaseAddress };
        await Csrf(client, cookies);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/login", new { userName = employee.UserName, password = SecurityHost.Password })).StatusCode);
        await Csrf(client, cookies);
        foreach (var path in new[] { "/api/dashboard", "/api/categories", "/api/product-types", "/api/products", "/api/orders", "/api/customers", "/api/payment-options", "/api/delivery-methods", "/api/email-settings", "/api/billing/company", "/api/shop-appearance", "/api/seo-settings" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/categories", new { name = "Staff category" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/company-access")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/company-access/roles/" + role.Id, new SaveCompanyRole("No sections", [], role.Revision))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/categories", new { name = "Denied" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/dashboard")).StatusCode);
    }
    [SecuritySqlFact]
    public async Task DelegatedPermissionsAreEnforcedForReadsWritesAndRoleManagementAndChangeImmediately()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var role = await CreateRole(host.Client, "Catalogus", ["products"]);
        var employee = await CreateAccount(host.Client, "catalog-user", [role.Id]);
        var cookies = new CookieContainer();
        using var client = new HttpClient(new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = false }) { BaseAddress = host.Client.BaseAddress };
        await Csrf(client, cookies);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/login", new { userName = employee.UserName, password = SecurityHost.Password })).StatusCode);
        await Csrf(client, cookies);
        var session = await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.True(session.GetProperty("businessAdministrator").GetBoolean());
        Assert.False(session.GetProperty("administrator").GetBoolean());
        Assert.Equal("products", Assert.Single(session.GetProperty("permissions").EnumerateArray()).GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/product-types")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/billing/vat-rates")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/categories", new { name = "Forbidden" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/company-access")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/company-access/roles", new SaveCompanyRole("Escalate", ["orders"]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/orders/" + Guid.NewGuid() + "/status", new { status = "paid", revision = Guid.NewGuid(), paymentReference = "test" })).StatusCode);
        var update = await host.Client.PutAsJsonAsync("/api/company-access/roles/" + role.Id, new SaveCompanyRole("Orders", ["orders"], role.Revision));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updatedRole = (await update.Content.ReadFromJsonAsync<CompanyRole>())!;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/company-access/roles/" + role.Id, new SaveCompanyRole("Stale", ["products"], role.Revision))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/company-access/roles/" + role.Id, new SaveCompanyRole("Escalate", ["companyAccess"], updatedRole.Revision))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/company-access/accounts/" + employee.Id, new UpdateCompanyAccount(employee.Revision, false, [role.Id]))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/orders")).StatusCode);
        await Csrf(client, cookies);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { userName = employee.UserName, password = SecurityHost.Password })).StatusCode);
    }

    [SecuritySqlFact]
    public async Task MultipleRolesCombineAndAccountManagementCannotModifyTheOwnerOrPromoteCustomers()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); await host.Login(); await host.Csrf();
        var orders = await CreateRole(host.Client, "Orders", ["orders"]);
        var customers = await CreateRole(host.Client, "Customers", ["customers"]);
        var employee = await CreateAccount(host.Client, "combined-user", [orders.Id, customers.Id]);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var access = scope.ServiceProvider.GetRequiredService<ICompanyAccess>();
            Assert.Equal(["customers", "orders"], (await access.PermissionsAsync(employee.Id, CancellationToken.None)).Order().ToArray());
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var owner = (await users.FindByNameAsync("admin"))!;
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/company-access/accounts/" + owner.Id, new UpdateCompanyAccount(owner.ConcurrencyStamp!, false, [orders.Id]))).StatusCode);
            var adminRole = (await scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>().FindByNameAsync("Administrator"))!;
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/company-access/accounts", new CreateCompanyAccount("escalate", "another@example.test", SecurityHost.Password, [adminRole.Id]))).StatusCode);
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await roleManager.CreateAsync(new("Customer"))).Succeeded);
            Assert.True((await users.AddToRoleAsync(owner, "Customer")).Succeeded);
            Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/customers/" + owner.Id)).StatusCode);
            Assert.True(await users.IsInRoleAsync(owner, "Administrator"));
        }
        var changed = await host.Client.PutAsJsonAsync("/api/company-access/accounts/" + employee.Id, new UpdateCompanyAccount(employee.Revision, true, [orders.Id]));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/company-access/accounts/" + employee.Id, new UpdateCompanyAccount(employee.Revision, false, [customers.Id]))).StatusCode);
        var page = (await host.Client.GetFromJsonAsync<JsonElement>("/api/company-access"));
        Assert.Equal(employee.UserName, Assert.Single(page.GetProperty("accounts").EnumerateArray()).GetProperty("userName").GetString());
    }

    private static async Task<CompanyRole> CreateRole(HttpClient client, string name, string[] permissions)
    {
        var response = await client.PostAsJsonAsync("/api/company-access/roles", new SaveCompanyRole(name, permissions));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompanyRole>())!;
    }
    private static async Task<CompanyAccount> CreateAccount(HttpClient client, string userName, string[] roleIds)
    {
        var response = await client.PostAsJsonAsync("/api/company-access/accounts", new CreateCompanyAccount(userName, "staff@example.test", SecurityHost.Password, roleIds));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompanyAccount>())!;
    }
    private static async Task Csrf(HttpClient client, CookieContainer cookies)
    {
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/auth/csrf")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", cookies.GetCookies(client.BaseAddress!)["XSRF-TOKEN"]!.Value);
    }
}
