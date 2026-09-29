using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Security;

public static class AdminAccountCommand
{
    public static async Task RunAsync(IServiceProvider services, bool reset)
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("Gebruik een interactieve terminal; geef wachtwoorden nooit mee als argument.");
        Console.Write("Gebruikersnaam: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Gebruikersnaam ontbreekt.");
        Console.Write("Nieuw wachtwoord (12–128 tekens, hoofdletter, kleine letter, cijfer en symbool): ");
        var password = ReadPassword();
        Console.Write("Herhaal wachtwoord: ");
        if (password != ReadPassword()) throw new InvalidOperationException("Wachtwoorden komen niet overeen.");
        if (password.Length > 128) throw new InvalidOperationException("Wachtwoord is te lang.");
        await ApplyAsync(services, name, password, reset);
        Console.WriteLine(reset ? "Wachtwoord gewijzigd; bestaande sessies zijn ongeldig." : "Beheerder aangemaakt.");
    }

    public static async Task ApplyAsync(IServiceProvider services, string name, string password, bool reset)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 256 || password.Length > 128)
            throw new InvalidOperationException("Ongeldige gebruikersnaam of wachtwoordlengte.");
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var existing = await users.FindByNameAsync(name);
        if (reset)
        {
            if (existing is null || !await users.IsInRoleAsync(existing, AdminSecurity.Role))
                throw new InvalidOperationException("Beheerder niet gevonden.");
            Check(await users.ResetPasswordAsync(existing, await users.GeneratePasswordResetTokenAsync(existing), password));
            Check(await users.SetLockoutEndDateAsync(existing, null));
            Check(await users.ResetAccessFailedCountAsync(existing));
        }
        else
        {
            if (existing is not null) throw new InvalidOperationException("Gebruikersnaam bestaat al; bestaand account is niet gewijzigd.");
            if (!await roles.RoleExistsAsync(AdminSecurity.Role)) Check(await roles.CreateAsync(new IdentityRole(AdminSecurity.Role)));
            var user = new IdentityUser(name);
            Check(await users.CreateAsync(user, password));
            Check(await users.AddToRoleAsync(user, AdminSecurity.Role));
        }
        await transaction.CommitAsync();
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
    private static string ReadPassword()
    {
        var value = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
            if (key.Key == ConsoleKey.Backspace) { if (value.Length > 0) value.Length--; }
            else if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
        }
    }
}
