using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace MyShop.Api.Security;

public static class AdminEndpoints
{
    private static readonly IdentityUser MissingUser = new("missing");
    private static readonly string MissingUserHash = new PasswordHasher<IdentityUser>().HashPassword(MissingUser, Guid.NewGuid().ToString());

    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false, SameSite = SameSiteMode.Strict, Secure = context.Request.IsHttps, Path = "/"
            });
            return Results.NoContent();
        }).AllowAnonymous();
        app.MapGet("/api/auth/session", (HttpContext context) => Results.Ok(new
        {
            authenticated = context.User.Identity?.IsAuthenticated == true,
            name = context.User.Identity?.Name,
            administrator = context.User.IsInRole(AdminSecurity.Role),
            customer = context.User.IsInRole(AdminSecurity.CustomerRole)
        })).AllowAnonymous();
        app.MapPost("/api/auth/login", Login).AllowAnonymous().RequireRateLimiting("admin-login");
        app.MapPost("/api/auth/logout", async (HttpContext context, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn) =>
        {
            var user = await users.GetUserAsync(context.User);
            if (user is not null && !(await users.UpdateSecurityStampAsync(user)).Succeeded)
                return Results.Problem(statusCode: 503);
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireAuthenticatedUser());
        app.MapPost("/api/auth/password", async (PasswordRequest request, HttpContext context, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn) =>
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length > 128 || request.CurrentPassword.Length > 128)
                return Results.BadRequest();
            var user = await users.GetUserAsync(context.User);
            if (user is null) return Results.Unauthorized();
            var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded) return Results.BadRequest(new { message = "Controleer het huidige wachtwoord en de eisen voor het nieuwe wachtwoord." });
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireAuthenticatedUser());
    }

    private static async Task<IResult> Login(LoginRequest request, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || request.UserName.Length > 256 || string.IsNullOrEmpty(request.Password) || request.Password.Length > 128)
            return Results.Unauthorized();
        var user = await users.FindByNameAsync(request.UserName.Trim());
        if (user is null)
        {
            // Perform password hashing work for unknown names as well.
            users.PasswordHasher.VerifyHashedPassword(MissingUser, MissingUserHash, request.Password);
            return Results.Unauthorized();
        }
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded || user.TwoFactorEnabled || !await users.IsInRoleAsync(user, AdminSecurity.Role)) return Results.Unauthorized();
        await signIn.SignInAsync(user, isPersistent: false);
        return Results.NoContent();
    }
}
public sealed record LoginRequest(string UserName, string Password);
public sealed record PasswordRequest(string CurrentPassword, string NewPassword);
