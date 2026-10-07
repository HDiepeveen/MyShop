using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Customers.GetCustomerProfile;
using MyShop.Application.Customers.UpdateCustomerProfile;

namespace MyShop.Api.Security;

public static class CustomerEndpoints
{
    private static readonly IdentityUser MissingUser = new("missing");
    private static readonly string MissingUserHash = new PasswordHasher<IdentityUser>()
        .HashPassword(MissingUser, Guid.NewGuid().ToString());

    public static void MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/customer/auth/register", RegisterAsync).AllowAnonymous()
            .RequireRateLimiting("customer-auth");
        endpoints.MapPost("/api/customer/auth/login", LoginAsync).AllowAnonymous()
            .RequireRateLimiting("customer-auth");
        endpoints.MapGet("/api/customer/profile", GetProfileAsync)
            .RequireAuthorization(AdminSecurity.CustomerPolicy);
        endpoints.MapPut("/api/customer/profile", UpdateProfileAsync)
            .RequireAuthorization(AdminSecurity.CustomerPolicy);
    }

    private static async Task<IResult> RegisterAsync(CustomerRegistrationRequest request,
        UserManager<IdentityUser> users, RoleManager<IdentityRole> roles,
        SignInManager<IdentityUser> signIn, MyShop.Application.Notifications.IEmailQueue emails,
        IConfiguration configuration, IHostEnvironment environment,
        MyShop.Infrastructure.Persistence.MyShopDbContext context, CancellationToken cancellationToken)
    {
        if (!ValidCredentials(request.Email, request.Password)) return InvalidRegistration();
        var email = request.Email.Trim();
        if (!await roles.RoleExistsAsync(AdminSecurity.CustomerRole))
        {
            var roleResult = await roles.CreateAsync(new IdentityRole(AdminSecurity.CustomerRole));
            if (!roleResult.Succeeded && !await roles.RoleExistsAsync(AdminSecurity.CustomerRole))
                return Results.Problem(statusCode: 503);
        }
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var user = new IdentityUser { UserName = email, Email = email };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return created.Errors.Any(error => error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                ? Results.Conflict(new { code = "accountExists", message = "Voor dit e-mailadres bestaat al een account." })
                : InvalidRegistration();
        var assigned = await users.AddToRoleAsync(user, AdminSecurity.CustomerRole);
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(user);
            return Results.Problem(statusCode: 503);
        }
        await CustomerEmailEndpoints.QueueConfirmationAsync(user, users, emails, configuration, environment, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await signIn.SignInAsync(user, isPersistent: false);
        return Results.NoContent();
    }

    private static async Task<IResult> LoginAsync(CustomerLoginRequest request,
        UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn)
    {
        if (!ValidCredentials(request.Email, request.Password)) return Results.Unauthorized();
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            users.PasswordHasher.VerifyHashedPassword(MissingUser, MissingUserHash, request.Password);
            return Results.Unauthorized();
        }
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded || user.TwoFactorEnabled || !await users.IsInRoleAsync(user, AdminSecurity.CustomerRole))
            return Results.Unauthorized();
        await signIn.SignInAsync(user, isPersistent: false);
        return Results.NoContent();
    }

    private static async Task<IResult> GetProfileAsync(HttpContext context,
        UserManager<IdentityUser> users, [FromServices] GetCustomerProfile useCase,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();
        var profile = await useCase.ExecuteAsync(user.Id, cancellationToken);
        return Results.Ok(new CustomerProfileResponse(user.Email!, profile?.Name,
            profile?.AddressLine, profile?.PostalCode, profile?.City, profile?.CountryCode,
            profile?.Revision, user.EmailConfirmed));
    }

    private static async Task<IResult> UpdateProfileAsync(CustomerProfileRequest request,
        HttpContext context, UserManager<IdentityUser> users,
        [FromServices] UpdateCustomerProfile useCase, CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();
        try
        {
            var result = await useCase.ExecuteAsync(new(user.Id, request.Name, request.AddressLine,
                request.PostalCode, request.City, request.CountryCode, request.Revision), cancellationToken);
            return result.Failure is UpdateCustomerProfileFailure.ConcurrencyConflict
                ? Results.Conflict(new { code = "concurrency", message = "Je profiel is intussen gewijzigd. Vernieuw de pagina." })
                : Results.Ok(new CustomerProfileResponse(user.Email!, result.Profile!.Name,
                    result.Profile.AddressLine, result.Profile.PostalCode, result.Profile.City,
                    result.Profile.CountryCode, result.Profile.Revision, user.EmailConfirmed));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { code = "invalidProfile", message = exception.Message });
        }
    }

    private static bool ValidCredentials(string email, string password) =>
        !string.IsNullOrWhiteSpace(email) && email.Length <= 320
        && !string.IsNullOrEmpty(password) && password.Length <= 128;
    private static IResult InvalidRegistration() => Results.BadRequest(new
    {
        code = "invalidRegistration",
        message = "Gebruik een geldig e-mailadres en een wachtwoord van minimaal 12 tekens met hoofdletter, kleine letter, cijfer en symbool."
    });
}

public sealed record CustomerRegistrationRequest(string Email, string Password);
public sealed record CustomerLoginRequest(string Email, string Password);
public sealed record CustomerProfileRequest(string Name, string AddressLine, string PostalCode,
    string City, string CountryCode, Guid? Revision);
public sealed record CustomerProfileResponse(string Email, string? Name, string? AddressLine,
    string? PostalCode, string? City, string? CountryCode, Guid? Revision, bool EmailConfirmed = false);
