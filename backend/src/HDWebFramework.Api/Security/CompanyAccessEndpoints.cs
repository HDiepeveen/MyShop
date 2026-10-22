using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Security;

namespace HDWebFramework.Api.Security;

public static class CompanyAccessEndpoints
{
    public static void MapCompanyAccess(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/company-access").RequireAuthorization(policy => policy.RequireRole(AdminSecurity.Role));
        group.MapGet("", async ([FromServices] ICompanyAccess access, CancellationToken token) => Results.Ok(new
        {
            permissions = CompanyPermissions.Items.Select(item => new { code = item.Key, name = item.Value }),
            roles = await access.RolesAsync(token), accounts = await access.AccountsAsync(token),
        }));
        group.MapPost("/roles", (SaveCompanyRole value, [FromServices] ICompanyAccess access, CancellationToken token) =>
            Execute(async () => Results.Ok(await access.SaveRoleAsync(null, value, token))));
        group.MapPut("/roles/{id}", (string id, SaveCompanyRole value, [FromServices] ICompanyAccess access, CancellationToken token) =>
            Execute(async () => Results.Ok(await access.SaveRoleAsync(id, value, token))));
        group.MapPost("/accounts", (CreateCompanyAccount value, [FromServices] ICompanyAccess access, CancellationToken token) =>
            Execute(async () => Results.Ok(await access.CreateAccountAsync(value, token))));
        group.MapPut("/accounts/{id}", (string id, UpdateCompanyAccount value, [FromServices] ICompanyAccess access, CancellationToken token) =>
            Execute(async () => Results.Ok(await access.UpdateAccountAsync(id, value, token))));
    }
    private static async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (CompanyAccessConflictException) { return Results.Conflict(new { code = "concurrencyConflict", message = "De gegevens zijn intussen gewijzigd. Vernieuw de pagina." }); }
        catch (ArgumentException exception) { return Results.BadRequest(new { code = "invalidAccess", message = exception.Message }); }
    }
}
