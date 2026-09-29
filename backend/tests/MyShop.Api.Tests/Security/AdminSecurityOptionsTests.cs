using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyShop.Api.Security;

namespace MyShop.Api.Tests.Security;

public sealed class AdminSecurityOptionsTests
{
    [Fact]
    public void ProductionRequiresSecureCookiesAndAdministratorRole()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAdminSecurity(false);
        using var provider = services.BuildServiceProvider();
        var cookie = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
        Assert.Equal(CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, cookie.Cookie.SameSite);
        Assert.False(cookie.SlidingExpiration);
        Assert.Equal(TimeSpan.FromMinutes(30), cookie.ExpireTimeSpan);
        var csrf = provider.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;
        Assert.Equal(CookieSecurePolicy.Always, csrf.Cookie.SecurePolicy);
        Assert.Equal("X-XSRF-TOKEN", csrf.HeaderName);
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value.FallbackPolicy!;
        var role = Assert.Single(policy.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>());
        Assert.Equal(AdminSecurity.Role, Assert.Single(role.AllowedRoles));
    }
}
