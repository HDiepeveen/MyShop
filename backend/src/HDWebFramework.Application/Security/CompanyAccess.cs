namespace HDWebFramework.Application.Security;

public static class CompanyPermissions
{
    public const string EmployeeRole = "BusinessAdministrator";
    public const string RolePrefix = "Company:";
    public const string Claim = "HDWebFramework.ManagementPermission";
    public static readonly IReadOnlyDictionary<string, string> Items = new Dictionary<string, string>
    {
        ["overview"] = "Overzicht", ["categories"] = "Categorieën", ["productTypes"] = "Producttypen",
        ["products"] = "Producten", ["orders"] = "Bestellingen", ["customers"] = "Klanten",
        ["paymentSettings"] = "Betaalinstellingen", ["payments"] = "Betaalopties", ["delivery"] = "Bezorgopties", ["email"] = "E-mailinstellingen",
        ["billing"] = "Facturatie en btw", ["appearance"] = "Vormgeving", ["seo"] = "Website en SEO",
    };
}
public sealed record CompanyRole(string Id, string Name, string Revision, IReadOnlyList<string> Permissions);
public sealed record CompanyAccount(string Id, string UserName, string Email, string Revision, bool Enabled, IReadOnlyList<string> Roles);
public sealed record SaveCompanyRole(string Name, string[] Permissions, string? Revision = null);
public sealed record CreateCompanyAccount(string UserName, string Email, string Password, string[] Roles);
public sealed record UpdateCompanyAccount(string Revision, bool Enabled, string[] Roles);
public sealed class CompanyAccessConflictException : Exception;
public interface ICompanyAccess
{
    Task<IReadOnlyList<string>> PermissionsAsync(string userId, CancellationToken token);
    Task<IReadOnlyList<CompanyRole>> RolesAsync(CancellationToken token);
    Task<CompanyRole> SaveRoleAsync(string? id, SaveCompanyRole value, CancellationToken token);
    Task<IReadOnlyList<CompanyAccount>> AccountsAsync(CancellationToken token);
    Task<CompanyAccount> CreateAccountAsync(CreateCompanyAccount value, CancellationToken token);
    Task<CompanyAccount> UpdateAccountAsync(string id, UpdateCompanyAccount value, CancellationToken token);
}
