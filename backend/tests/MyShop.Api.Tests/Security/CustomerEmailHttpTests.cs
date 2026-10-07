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
public sealed class CustomerEmailHttpTests
{
    [Fact]
    public async Task Registration_queues_confirmation_and_link_confirms_only_the_matching_customer()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "verify@example.test", password = SecurityHost.Password })).StatusCode);
        string body;
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            body = await db.Database.SqlQueryRaw<string>("SELECT Body AS Value FROM EmailMessages WHERE Recipient = 'verify@example.test'").SingleAsync();
        }
        var link = body.Split('\n').Single(line => line.StartsWith("http://")).Trim();
        var query = QueryHelpers.ParseQuery(new Uri(link).Query);
        await host.Csrf();
        var profile = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/profile");
        Assert.False(profile.GetProperty("emailConfirmed").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/confirm-email", new { userId = query["userId"].ToString(), token = "invalid" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/confirm-email", new { userId = query["userId"].ToString(), token = query["token"].ToString() })).StatusCode);
        profile = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/profile");
        Assert.True(profile.GetProperty("emailConfirmed").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/request-confirmation", new { email = "verify@example.test" })).StatusCode);
        await using var check = host.App.Services.CreateAsyncScope();
        Assert.Equal(1, await check.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM EmailMessages WHERE Recipient = 'verify@example.test'").SingleAsync());
    }

    [Fact]
    public async Task Reset_is_customer_scoped_single_use_and_revokes_the_previous_session()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "reset@example.test", password = SecurityHost.Password })).StatusCode);
        await host.Csrf();
        foreach (var email in new[] { "unknown@example.test", "", "admin@example.test", "reset@example.test" })
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/request-password-reset", new { email })).StatusCode);
        string body;
        await using (var scope = host.App.Services.CreateAsyncScope())
            body = await scope.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.SqlQueryRaw<string>("SELECT Body AS Value FROM EmailMessages WHERE Subject = N'Herstel je MyShop wachtwoord'").SingleAsync();
        var query = QueryHelpers.ParseQuery(new Uri(body.Split('\n').Single(line => line.StartsWith("http://")).Trim()).Query);
        var request = new { userId = query["userId"].ToString(), token = query["token"].ToString(), password = "NewStrongPassword123!" };
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/reset-password", new { request.userId, request.token, password = "weak" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/reset-password", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/customer/profile")).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/reset-password", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PostAsJsonAsync("/api/customer/auth/login", new { email = "reset@example.test", password = SecurityHost.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/login", new { email = "reset@example.test", request.password })).StatusCode);
    }

    [Fact]
    public async Task Account_email_writes_require_csrf_and_do_not_reset_administrators()
    {
        await using var host = await SecurityHost.Create();
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/request-password-reset", new { email = "admin@example.test" })).StatusCode);
        string token; string id;
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var admin = (await users.FindByNameAsync("admin"))!;
            admin.Email = "admin@example.test"; Assert.True((await users.UpdateAsync(admin)).Succeeded);
            Assert.True((await scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>().CreateAsync(new IdentityRole("Customer"))).Succeeded);
            Assert.True((await users.AddToRoleAsync(admin, "Customer")).Succeeded);
            id = admin.Id;
            token = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(await users.GeneratePasswordResetTokenAsync(admin)));
        }
        await host.Csrf();
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/reset-password", new { userId = id, token, password = "NewStrongPassword123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/request-password-reset", new { email = "admin@example.test" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
    }
    [Fact]
    public async Task Registration_rolls_back_when_the_confirmation_cannot_be_queued()
    {
        await using var host = await SecurityHost.Create(new() { ["Email:PublicBaseUrl"] = "http://untrusted.example" });
        await host.Csrf();
        Assert.Equal(HttpStatusCode.InternalServerError, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "rollback@example.test", password = SecurityHost.Password })).StatusCode);
        await using var scope = host.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        Assert.False(await db.Users.AnyAsync(user => user.Email == "rollback@example.test"));
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM EmailMessages WHERE Recipient = 'rollback@example.test'").SingleAsync());
    }

    [Fact]
    public async Task Token_purposes_and_expiration_are_enforced()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "purpose@example.test", password = SecurityHost.Password })).StatusCode);
        string reset; string confirm; string id; string otherId;
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = (await users.FindByEmailAsync("purpose@example.test"))!;
            id = user.Id;
            reset = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(await users.GeneratePasswordResetTokenAsync(user)));
            confirm = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));
            var other = new IdentityUser { UserName = "other@example.test", Email = "other@example.test" };
            Assert.True((await users.CreateAsync(other, SecurityHost.Password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(other, "Customer")).Succeeded);
            otherId = other.Id;
        }
        await host.Csrf();
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/confirm-email", new { userId = otherId, token = confirm })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/reset-password", new { userId = otherId, token = reset, password = "NewStrongPassword123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/confirm-email", new { userId = id, token = reset })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/reset-password", new { userId = id, token = confirm, password = "NewStrongPassword123!" })).StatusCode);
        host.App.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DataProtectionTokenProviderOptions>>().Value.TokenLifespan = TimeSpan.FromSeconds(-1);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/confirm-email", new { userId = id, token = confirm })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/customer/auth/reset-password", new { userId = id, token = reset, password = "NewStrongPassword123!" })).StatusCode);
    }
}
