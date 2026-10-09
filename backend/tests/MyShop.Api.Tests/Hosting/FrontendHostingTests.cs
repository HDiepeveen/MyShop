using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api;

namespace MyShop.Api.Tests.Hosting;

public sealed class FrontendHostingTests
{
    [Fact]
    public async Task Frontend_is_public_deep_links_work_and_api_and_configuration_are_not_exposed()
    {
        var folder = Path.Combine(Path.GetTempPath(), "MyShopFrontendTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "index.html"), "<html>MyShop frontend</html>");
        await File.WriteAllTextAsync(Path.Combine(folder, "main.js"), "console.log('MyShop');");
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = folder });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
                options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; });
            builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().RequireRole("Administrator").Build());
            await using var app = builder.Build();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/api/private", () => "Private");
            app.MapFrontend();
            await app.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            foreach (var route in new[] { "/", "/winkel", "/winkel/registreren", "/producten/123" })
            {
                var response = await client.GetAsync(route);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
                Assert.Equal("<html>MyShop frontend</html>", await response.Content.ReadAsStringAsync());
            }
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/main.js")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/private")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/unknown")).StatusCode);
            Assert.NotEqual(HttpStatusCode.OK, (await client.GetAsync("/appsettings.Production.json")).StatusCode);
            Assert.NotEqual(HttpStatusCode.OK, (await client.GetAsync("/App_Data/keys/key.xml")).StatusCode);
            await app.StopAsync();
        }
        finally { Directory.Delete(folder, true); }
    }
}
