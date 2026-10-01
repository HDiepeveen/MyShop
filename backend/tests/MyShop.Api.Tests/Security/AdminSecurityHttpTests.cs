using MyShop.Api.Storefront;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyShop.Api.Catalog;
using MyShop.Api.Checkout;
using MyShop.Api.Security;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Security;

public sealed class SecuritySqlFactAttribute : FactAttribute
{
    public SecuritySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MYSHOP_TEST_SQLSERVER")))
            Skip = "Set MYSHOP_TEST_SQLSERVER to run authentication integration tests.";
    }
}

public sealed class AdminSecurityHttpTests
{
    [SecuritySqlFact]
    public async Task EveryCatalogWriteRequiresAntiforgeryEvenForAdministrator()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        host.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        var routes = ((IEndpointRouteBuilder)host.App).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => !e.RoutePattern.RawText!.StartsWith("/api/auth")
                && !e.RoutePattern.RawText.StartsWith("/api/customer/")
                && !e.RoutePattern.RawText.StartsWith("/api/shop/"));
        foreach (var endpoint in routes)
        foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Where(m => m != "GET"))
        {
            var path = Regex.Replace(endpoint.RoutePattern.RawText!, @"\{[^}]+\}", Guid.NewGuid().ToString());
            using var response = await host.Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("csrf", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }

    [SecuritySqlFact]
    public async Task EveryCatalogEndpointRejectsAnonymousRequestsWithoutRedirects()
    {
        await using var host = await SecurityHost.Create();
        var routes = ((IEndpointRouteBuilder)host.App).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => !e.RoutePattern.RawText!.StartsWith("/api/auth")
                && !e.RoutePattern.RawText.StartsWith("/api/customer/")
                && !e.RoutePattern.RawText.StartsWith("/api/shop/"));
        Assert.NotEmpty(routes);
        foreach (var endpoint in routes)
        foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
        {
            var path = Regex.Replace(endpoint.RoutePattern.RawText!, @"\{[^}]+\}", Guid.NewGuid().ToString());
            using var response = await host.Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }
        Assert.DoesNotContain(((IEndpointRouteBuilder)host.App).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>(), e => e.RoutePattern.RawText == "/api/auth/register");
    }

    [SecuritySqlFact]
    public async Task LoginCsrfPasswordChangeAndLogoutInvalidateOldSessions()
    {
        await using var host = await SecurityHost.Create();
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/auth/login", new { userName = "admin", password = SecurityHost.Password })).StatusCode);
        await host.Csrf();
        var login = await host.Login();
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith("MyShop.Admin="));
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=strict", cookie.ToLowerInvariant());
        Assert.DoesNotContain("expires=", cookie.ToLowerInvariant());
        var oldCookie = cookie.Split(';')[0];
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/products")).StatusCode);
        // The anonymous antiforgery token cannot be reused after login.
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/categories", new { name = "Protected" })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Created, (await host.Client.PostAsJsonAsync("/api/categories", new { name = "Protected" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/auth/password", new { currentPassword = "wrong", newPassword = "New-Strong-Password42!" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/auth/password", new { currentPassword = SecurityHost.Password, newPassword = "New-Strong-Password42!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await host.ReplayCookie(oldCookie));
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Login()).StatusCode);
        var newLogin = await host.Login("New-Strong-Password42!");
        Assert.Equal(HttpStatusCode.NoContent, newLogin.StatusCode);
        var newCookie = newLogin.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith("MyShop.Admin=")).Split(';')[0];
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await host.ReplayCookie(newCookie));
    }

    [SecuritySqlFact]
    public async Task RemovingRoleRevokesAccessOnNextRequestAndSessionReflectsIt()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.True((await users.RemoveFromRoleAsync((await users.FindByNameAsync("admin"))!, AdminSecurity.Role)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/products")).StatusCode);
        var session = await host.Client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.False(session.GetProperty("administrator").GetBoolean());
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Login()).StatusCode);
    }

    [SecuritySqlFact]
    public async Task SessionExpiresAfterThirtyMinutesEvenWhileActivelyUsed()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        host.Clock.Advance(TimeSpan.FromMinutes(15));
        var active = await host.Client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        Assert.False(active.Headers.Contains("Set-Cookie"));
        host.Clock.Advance(TimeSpan.FromMinutes(16));
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/products")).StatusCode);
    }

    [SecuritySqlFact]
    public async Task LocalAccountProvisioningRejectsDuplicatesAndRecoveryRevokesSessions()
    {
        await using var host = await SecurityHost.Create();
        await Assert.ThrowsAsync<InvalidOperationException>(() => AdminAccountCommand.ApplyAsync(host.App.Services, "admin", "Replacement-Password42!", false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => AdminAccountCommand.ApplyAsync(host.App.Services, "new-admin", "weak", false));
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.Null(await users.FindByNameAsync("new-admin"));
        }
        await AdminAccountCommand.ApplyAsync(host.App.Services, "new-admin", "New-Admin-Password42!", false);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = (await users.FindByNameAsync("new-admin"))!;
            Assert.True(await users.IsInRoleAsync(user, AdminSecurity.Role));
            Assert.NotEqual("New-Admin-Password42!", user.PasswordHash);
        }
        await host.Csrf();
        var login = await host.Login();
        var cookie = login.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith("MyShop.Admin=")).Split(';')[0];
        await AdminAccountCommand.ApplyAsync(host.App.Services, "admin", "Recovered-Password42!", true);
        Assert.Equal(HttpStatusCode.Unauthorized, await host.ReplayCookie(cookie));
    }

    [SecuritySqlFact]
    public async Task FailedPasswordsLockAccountAndLoginRateLimitIsEnforced()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.Unauthorized, (await host.Login("wrong")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Login()).StatusCode);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.True(await users.IsLockedOutAsync((await users.FindByNameAsync("admin"))!));
        }
        for (var i = 0; i < 4; i++) await host.Login("wrong");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await host.Login()).StatusCode);
    }

    [SecuritySqlFact]
    public async Task CustomerCanRegisterLoginAndMaintainOwnProfileWithoutAdminAccess()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        var registered = await host.Client.PostAsJsonAsync("/api/customer/auth/register", new
            { email = "customer@example.com", password = SecurityHost.Password });
        Assert.Equal(HttpStatusCode.NoContent, registered.StatusCode);
        var session = await host.Client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.True(session.GetProperty("customer").GetBoolean());
        Assert.False(session.GetProperty("administrator").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/products")).StatusCode);
        await host.Csrf();
        var saved = await host.Client.PutAsJsonAsync("/api/customer/profile", new
        {
            name = "Ada Lovelace", addressLine = "Straat 1", postalCode = "1234 AB",
            city = "Utrecht", countryCode = "nl", revision = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var profile = await saved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("NL", profile.GetProperty("countryCode").GetString());
        Assert.NotEqual(Guid.Empty, profile.GetProperty("revision").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsJsonAsync(
            "/api/customer/auth/register", new { email = "customer@example.com", password = SecurityHost.Password })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent,
            (await host.Client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync(
            "/api/customer/auth/login", new { email = "customer@example.com", password = SecurityHost.Password })).StatusCode);
        var loaded = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/profile");
        Assert.Equal("Ada Lovelace", loaded.GetProperty("name").GetString());
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/auth/password",
            new { currentPassword = SecurityHost.Password, newPassword = "New-Customer-Password42!" })).StatusCode);
    }
}

internal sealed class SecurityHost : IAsyncDisposable
{
    internal const string Password = "Test-only-Password42!";
    internal WebApplication App { get; private set; } = null!;
    internal HttpClient Client { get; private set; } = null!;
    internal AdjustableClock Clock { get; } = new();
    private readonly CookieContainer cookies = new();
    private readonly string databaseName = "MyShopTests_" + Guid.NewGuid().ToString("N");
    private string connection = "";

    internal static async Task<SecurityHost> Create()
    {
        var host = new SecurityHost();
        try { await host.Start(); return host; }
        catch { await host.DisposeAsync(); throw; }
    }
    private async Task Start()
    {
        var sql = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MYSHOP_TEST_SQLSERVER")) { InitialCatalog = databaseName };
        if (!string.IsNullOrEmpty(sql.AttachDBFilename)) throw new InvalidOperationException("AttachDBFilename is not supported.");
        connection = sql.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:MyShop"] = connection });
        builder.Services.AddMyShop(builder.Configuration);
        builder.Services.AddAdminSecurity(true);
        builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options => options.TimeProvider = Clock);
        App = builder.Build();
        App.UseAdminSecurity();
        App.MapAdminEndpoints();
        App.MapCustomerEndpoints();
        App.MapCatalog();
        App.MapStorefront();
        App.MapPaymentOptions();
        App.MapOrderManagement();
        await using (var scope = App.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.MigrateAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await roles.CreateAsync(new IdentityRole(AdminSecurity.Role))).Succeeded);
            var user = new IdentityUser("admin");
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, AdminSecurity.Role)).Succeeded);
        }
        await App.StartAsync();
        Client = new HttpClient(new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = false }) { BaseAddress = new Uri(App.Urls.Single()) };
    }
    internal async Task Csrf()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await Client.GetAsync("/api/auth/csrf")).StatusCode);
        Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", cookies.GetCookies(Client.BaseAddress!)["XSRF-TOKEN"]!.Value);
    }
    internal Task<HttpResponseMessage> Login(string password = Password) => Client.PostAsJsonAsync("/api/auth/login", new { userName = "admin", password });
    internal async Task<HttpStatusCode> ReplayCookie(string cookie)
    {
        using var client = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) { BaseAddress = Client.BaseAddress };
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return (await client.GetAsync("/api/products")).StatusCode;
    }
    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (App is not null) await App.DisposeAsync();
        if (connection.Length == 0) return;
        if (new SqlConnectionStringBuilder(connection).InitialCatalog != databaseName || !Guid.TryParseExact(databaseName[12..], "N", out _))
            throw new InvalidOperationException("Refusing to remove a database not owned by this test.");
        using var sql = new SqlConnection(connection);
        SqlConnection.ClearPool(sql);
        await using var db = new MyShopDbContext(new DbContextOptionsBuilder<MyShopDbContext>().UseSqlServer(connection).Options);
        await db.Database.EnsureDeletedAsync();
    }
}

internal sealed class AdjustableClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => now;
    internal void Advance(TimeSpan duration) => now += duration;
}
