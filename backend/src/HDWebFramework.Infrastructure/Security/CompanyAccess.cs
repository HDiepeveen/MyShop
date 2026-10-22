using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HDWebFramework.Application.Security;
using HDWebFramework.Infrastructure.Persistence;

namespace HDWebFramework.Infrastructure.Security;

public sealed class CompanyAccess(HDWebFrameworkDbContext db, UserManager<IdentityUser> users, RoleManager<IdentityRole> roles) : ICompanyAccess
{
    public async Task<IReadOnlyList<string>> PermissionsAsync(string userId, CancellationToken token)
    {
        var member = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, token);
        if (member is null || member.LockoutEnabled && member.LockoutEnd > DateTimeOffset.UtcNow) return [];
        var names = await users.GetRolesAsync(member);
        if (names.Contains("Administrator")) return CompanyPermissions.Items.Keys.ToArray();
        if (!names.Contains(CompanyPermissions.EmployeeRole)) return [];
        return await (from membership in db.UserRoles
                      join role in db.Roles on membership.RoleId equals role.Id
                      join claim in db.RoleClaims on role.Id equals claim.RoleId
                      where membership.UserId == userId && role.Name!.StartsWith(CompanyPermissions.RolePrefix)
                        && claim.ClaimType == CompanyPermissions.Claim
                      select claim.ClaimValue!).Distinct().ToArrayAsync(token);
    }

    public async Task<IReadOnlyList<CompanyRole>> RolesAsync(CancellationToken token)
    {
        var items = await db.Roles.AsNoTracking().Where(role => role.Name!.StartsWith(CompanyPermissions.RolePrefix))
            .OrderBy(role => role.Name).ToArrayAsync(token);
        var result = new List<CompanyRole>();
        foreach (var role in items) result.Add(await RoleAsync(role, token));
        return result;
    }

    public async Task<CompanyRole> SaveRoleAsync(string? id, SaveCompanyRole value, CancellationToken token)
    {
        var name = Required(value.Name, 80);
        if (value.Permissions is null || value.Permissions.Length > CompanyPermissions.Items.Count
            || value.Permissions.Any(permission => !CompanyPermissions.Items.ContainsKey(permission))
            || value.Permissions.Distinct().Count() != value.Permissions.Length)
            throw new ArgumentException("Kies geldige beheeronderdelen.");
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var role = id is null ? new IdentityRole(CompanyPermissions.RolePrefix + name) : await roles.FindByIdAsync(id);
        if (role is null || !role.Name!.StartsWith(CompanyPermissions.RolePrefix)) throw new ArgumentException("Onbekende bedrijfsrol.");
        if (id is null)
        {
            if (await db.Roles.CountAsync(item => item.Name!.StartsWith(CompanyPermissions.RolePrefix), token) >= 100)
                throw new ArgumentException("Er kunnen maximaal 100 bedrijfsrollen worden aangemaakt.");
            Check(await roles.CreateAsync(role));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(value.Revision) || role.ConcurrencyStamp != value.Revision) throw new CompanyAccessConflictException();
            role.Name = CompanyPermissions.RolePrefix + name;
            Check(await roles.UpdateAsync(role));
        }
        await db.RoleClaims.Where(claim => claim.RoleId == role.Id && claim.ClaimType == CompanyPermissions.Claim).ExecuteDeleteAsync(token);
        db.RoleClaims.AddRange(value.Permissions.Select(permission => new IdentityRoleClaim<string>
        { RoleId = role.Id, ClaimType = CompanyPermissions.Claim, ClaimValue = permission }));
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return await RoleAsync(role, token);
    }

    public async Task<IReadOnlyList<CompanyAccount>> AccountsAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var members = await users.GetUsersInRoleAsync(CompanyPermissions.EmployeeRole);
        var result = new List<CompanyAccount>();
        foreach (var member in members)
            if (!await users.IsInRoleAsync(member, "Administrator")) result.Add(await AccountAsync(member, token));
        return result.OrderBy(item => item.UserName).ToArray();
    }

    public async Task<CompanyAccount> CreateAccountAsync(CreateCompanyAccount value, CancellationToken token)
    {
        var name = Required(value.UserName, 100);
        var email = Required(value.Email, 320);
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email
            || string.IsNullOrEmpty(value.Password) || value.Password.Length > 128)
            throw new ArgumentException("Vul een geldig e-mailadres en wachtwoord in.");
        var assigned = await ValidateRolesAsync(value.Roles, token);
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (!await roles.RoleExistsAsync(CompanyPermissions.EmployeeRole)) Check(await roles.CreateAsync(new(CompanyPermissions.EmployeeRole)));
        var member = new IdentityUser(name) { Email = email, LockoutEnabled = true };
        Check(await users.CreateAsync(member, value.Password));
        Check(await users.AddToRolesAsync(member, assigned.Append(CompanyPermissions.EmployeeRole)));
        await transaction.CommitAsync(token);
        return await AccountAsync(member, token);
    }

    public async Task<CompanyAccount> UpdateAccountAsync(string id, UpdateCompanyAccount value, CancellationToken token)
    {
        var assigned = await ValidateRolesAsync(value.Roles, token);
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var member = await users.FindByIdAsync(id);
        if (member is null || !await users.IsInRoleAsync(member, CompanyPermissions.EmployeeRole)
            || await users.IsInRoleAsync(member, "Administrator")) throw new ArgumentException("Onbekende bedrijfsbeheerder.");
        if (string.IsNullOrWhiteSpace(value.Revision) || member.ConcurrencyStamp != value.Revision) throw new CompanyAccessConflictException();
        member.LockoutEnabled = true;
        member.LockoutEnd = value.Enabled ? null : DateTimeOffset.MaxValue;
        if (value.Enabled) member.AccessFailedCount = 0;
        Check(await users.UpdateAsync(member));
        var previous = (await users.GetRolesAsync(member)).Where(name => name.StartsWith(CompanyPermissions.RolePrefix)).ToArray();
        Check(await users.RemoveFromRolesAsync(member, previous));
        Check(await users.AddToRolesAsync(member, assigned));
        Check(await users.UpdateSecurityStampAsync(member));
        await transaction.CommitAsync(token);
        return await AccountAsync(member, token);
    }

    private async Task<string[]> ValidateRolesAsync(string[] ids, CancellationToken token)
    {
        if (ids is null || ids.Length is < 1 or > 100 || ids.Distinct().Count() != ids.Length) throw new ArgumentException("Kies minstens één bedrijfsrol.");
        var assigned = await db.Roles.Where(role => ids.Contains(role.Id) && role.Name!.StartsWith(CompanyPermissions.RolePrefix)).Select(role => role.Name!).ToArrayAsync(token);
        if (assigned.Length != ids.Length) throw new ArgumentException("Kies geldige bedrijfsrollen.");
        return assigned;
    }
    private async Task<CompanyRole> RoleAsync(IdentityRole role, CancellationToken token) => new(role.Id,
        role.Name![CompanyPermissions.RolePrefix.Length..], role.ConcurrencyStamp!,
        await db.RoleClaims.AsNoTracking().Where(claim => claim.RoleId == role.Id && claim.ClaimType == CompanyPermissions.Claim).Select(claim => claim.ClaimValue!).ToArrayAsync(token));
    private async Task<CompanyAccount> AccountAsync(IdentityUser member, CancellationToken token) => new(member.Id,
        member.UserName!, member.Email ?? "", member.ConcurrencyStamp!, !(member.LockoutEnd > DateTimeOffset.UtcNow),
        await (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id
               where membership.UserId == member.Id && role.Name!.StartsWith(CompanyPermissions.RolePrefix) select role.Id).ToArrayAsync(token));
    private static string Required(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximum
        ? value.Trim() : throw new ArgumentException("Controleer de ingevulde gegevens.");
    private static void Check(IdentityResult result)
    {
        if (result.Succeeded) return;
        if (result.Errors.Any(error => error.Code == "ConcurrencyFailure")) throw new CompanyAccessConflictException();
        throw new ArgumentException("Opslaan mislukt. Controleer of de naam uniek is en het wachtwoord aan de eisen voldoet.");
    }
}
