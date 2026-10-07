using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using MyShop.Application.Notifications;

namespace MyShop.Api.Security;

public static class CustomerEmailEndpoints
{
    public static void MapCustomerEmailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/customer/auth/request-password-reset", RequestResetAsync).AllowAnonymous().RequireRateLimiting("customer-auth");
        endpoints.MapPost("/api/customer/auth/reset-password", ResetAsync).AllowAnonymous().RequireRateLimiting("customer-auth");
        endpoints.MapPost("/api/customer/auth/request-confirmation", RequestConfirmationAsync).AllowAnonymous().RequireRateLimiting("customer-auth");
        endpoints.MapPost("/api/customer/auth/confirm-email", ConfirmAsync).AllowAnonymous().RequireRateLimiting("customer-auth");
    }

    internal static async Task QueueConfirmationAsync(IdentityUser user, UserManager<IdentityUser> users,
        IEmailQueue queue, IEmailSettingsRepository settings, IHostEnvironment environment, CancellationToken cancellationToken)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = Link("e-mail-bevestigen", user.Id, token, (await settings.GetAsync(cancellationToken)).PublicBaseUrl, environment);
        await queue.EnqueueAsync(user.Email!, "Bevestig je MyShop e-mailadres",
            $"Bevestig je e-mailadres via deze link:\n\n{link}\n\nDe link is twee uur geldig. Heb je geen MyShop-account gemaakt? Dan kun je deze e-mail negeren.", cancellationToken);
    }

    private static async Task<IResult> RequestConfirmationAsync(CustomerEmailRequest request, UserManager<IdentityUser> users,
        IEmailQueue queue, IEmailSettingsRepository settings, IHostEnvironment environment, CancellationToken cancellationToken)
    {
        var user = await CustomerAsync(request.Email, users);
        if (user is not null && !user.EmailConfirmed)
            await QueueConfirmationAsync(user, users, queue, settings, environment, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RequestResetAsync(CustomerEmailRequest request, UserManager<IdentityUser> users,
        IEmailQueue queue, IEmailSettingsRepository settings, IHostEnvironment environment, CancellationToken cancellationToken)
    {
        var user = await CustomerAsync(request.Email, users);
        if (user is not null && !await users.IsLockedOutAsync(user))
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var link = Link("wachtwoord-herstellen", user.Id, token, (await settings.GetAsync(cancellationToken)).PublicBaseUrl, environment);
            await queue.EnqueueAsync(user.Email!, "Herstel je MyShop wachtwoord",
                $"Kies een nieuw wachtwoord via deze link:\n\n{link}\n\nDe link is twee uur geldig. Heb je dit niet aangevraagd? Negeer deze e-mail; je wachtwoord blijft ongewijzigd.", cancellationToken);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> ConfirmAsync(CustomerTokenRequest request, UserManager<IdentityUser> users)
    {
        var user = await TokenCustomerAsync(request.UserId, users);
        var token = Decode(request.Token);
        if (user is null || token is null) return InvalidLink();
        var result = await users.ConfirmEmailAsync(user, token);
        return result.Succeeded ? Results.NoContent() : InvalidLink();
    }

    private static async Task<IResult> ResetAsync(CustomerResetRequest request, UserManager<IdentityUser> users)
    {
        var user = await TokenCustomerAsync(request.UserId, users);
        var token = Decode(request.Token);
        if (user is null || token is null || string.IsNullOrEmpty(request.Password) || request.Password.Length is < 12 or > 128
            || await users.IsLockedOutAsync(user)) return InvalidLink();
        var result = await users.ResetPasswordAsync(user, token, request.Password);
        return result.Succeeded ? Results.NoContent() : InvalidLink();
    }

    private static async Task<IdentityUser?> CustomerAsync(string? email, UserManager<IdentityUser> users)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320) return null;
        var user = await users.FindByEmailAsync(email.Trim());
        return user is not null && await users.IsInRoleAsync(user, AdminSecurity.CustomerRole)
            && !await users.IsInRoleAsync(user, AdminSecurity.Role) ? user : null;
    }
    private static async Task<IdentityUser?> TokenCustomerAsync(string? id, UserManager<IdentityUser> users)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 450) return null;
        var user = await users.FindByIdAsync(id);
        return user is not null && await users.IsInRoleAsync(user, AdminSecurity.CustomerRole)
            && !await users.IsInRoleAsync(user, AdminSecurity.Role) ? user : null;
    }
    private static string? Decode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return null;
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
        catch (FormatException) { return null; }
    }
    private static IResult InvalidLink() => Results.BadRequest(new { code = "invalidLink", message = "De link of het wachtwoord is ongeldig. Vraag een nieuwe link aan en gebruik een sterk wachtwoord van 12 tot en met 128 tekens." });
    private static string Link(string path, string userId, string token, string value, IHostEnvironment environment)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || (uri.Scheme != "https" && !(environment.IsDevelopment() && uri.Scheme == "http" && uri.IsLoopback)))
            throw new InvalidOperationException("Configure an HTTPS Email:PublicBaseUrl (local HTTP is allowed in Development).");
        return QueryHelpers.AddQueryString(value.TrimEnd('/') + "/winkel/" + path,
            new Dictionary<string, string?> { ["userId"] = userId, ["token"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)) });
    }
}

public sealed record CustomerEmailRequest(string Email);
public sealed record CustomerTokenRequest(string UserId, string Token);
public sealed record CustomerResetRequest(string UserId, string Token, string Password);
