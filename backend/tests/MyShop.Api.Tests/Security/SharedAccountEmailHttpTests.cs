using MyShop.Api.Security;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Security;

[Collection("SqlServer integration")]
public sealed class SharedAccountEmailHttpTests
{
    [SecuritySqlFact]
    public Task Shared_email_with_admin_email() => Shared_email_keeps_customer_registration_login_confirmation_and_reset_separate_from_admin(true);

    [SecuritySqlFact]
    public Task Shared_email_with_admin_username_only() => Shared_email_keeps_customer_registration_login_confirmation_and_reset_separate_from_admin(false);

    private async Task Shared_email_keeps_customer_registration_login_confirmation_and_reset_separate_from_admin(bool adminHasEmail)
    {
        const string email = "shared@example.test";
        const string customerPassword = "CustomerStrongPassword42!";
        const string newPassword = "NewCustomerStrongPassword42!";
        await using var host = await SecurityHost.Create();
        string adminId;
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var admin = (await users.FindByNameAsync("admin"))!;
            adminId = admin.Id;
            Assert.True((await users.SetUserNameAsync(admin, email)).Succeeded);
            if (adminHasEmail) Assert.True((await users.SetEmailAsync(admin, email)).Succeeded);
        }
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email, password = customerPassword })).StatusCode);
        await host.Csrf();
        var session = await host.Client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.True(session.GetProperty("customer").GetBoolean());
        Assert.False(session.GetProperty("administrator").GetBoolean());
        Assert.Equal(email, session.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = " SHARED@example.test ", password = customerPassword })).StatusCode);
        string body;
        await using (var scope = host.App.Services.CreateAsyncScope())
            body = await scope.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.SqlQueryRaw<string>("SELECT Body AS Value FROM EmailMessages WHERE Subject = N'Bevestig je MyShop e-mailadres'").SingleAsync();
        var confirmation = QueryHelpers.ParseQuery(new Uri(body.Split('\n').Single(line => line.StartsWith("http://")).Trim()).Query);
        var customerId = confirmation["userId"].ToString();
        Assert.NotEqual(adminId, customerId);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/confirm-email", new { userId = customerId, token = confirmation["token"].ToString() })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PostAsJsonAsync("/api/customer/auth/login", new { email, password = SecurityHost.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/login", new { email = "SHARED@example.test", password = customerPassword })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/request-password-reset", new { email })).StatusCode);
        await using (var scope = host.App.Services.CreateAsyncScope())
            body = await scope.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.SqlQueryRaw<string>("SELECT Body AS Value FROM EmailMessages WHERE Subject = N'Herstel je MyShop wachtwoord'").SingleAsync();
        var reset = QueryHelpers.ParseQuery(new Uri(body.Split('\n').Single(line => line.StartsWith("http://")).Trim()).Query);
        Assert.Equal(customerId, reset["userId"].ToString());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/reset-password", new { userId = customerId, token = reset["token"].ToString(), password = newPassword })).StatusCode);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var admin = (await users.FindByIdAsync(adminId))!;
            Assert.True(await users.CheckPasswordAsync(admin, SecurityHost.Password));
            Assert.False(await users.CheckPasswordAsync(admin, newPassword));
            Assert.False(await users.IsInRoleAsync(admin, AdminSecurity.CustomerRole));
            var customer = (await users.FindByIdAsync(customerId))!;
            Assert.True(customer.EmailConfirmed);
            Assert.True(await users.CheckPasswordAsync(customer, newPassword));
            Assert.False(await users.IsInRoleAsync(customer, AdminSecurity.Role));
        }
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/auth/login", new { userName = email, password = SecurityHost.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/products")).StatusCode);
    }

    [SecuritySqlFact]
    public async Task Existing_customer_email_usernames_continue_working_and_cannot_be_registered_again()
    {
        const string email = "legacy@example.test";
        await using var host = await SecurityHost.Create();
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await roles.CreateAsync(new IdentityRole(AdminSecurity.CustomerRole))).Succeeded);
            var customer = new IdentityUser { UserName = email, Email = email };
            Assert.True((await users.CreateAsync(customer, SecurityHost.Password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(customer, AdminSecurity.CustomerRole)).Succeeded);
            var admin = (await users.FindByNameAsync("admin"))!;
            Assert.True((await users.SetEmailAsync(admin, email)).Succeeded);
        }
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/login", new { email, password = SecurityHost.Password })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email, password = SecurityHost.Password })).StatusCode);
    }
}
