using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace HDWebFramework.Infrastructure.Security;

// Old identifiers are read only to move existing encrypted settings to the new product identity.
internal static class FrameworkSecretProtection
{
    internal static (string Value, string? Replacement) Read(IDataProtectionProvider provider,
        string purpose, string encrypted, IConfiguration configuration, IHostEnvironment environment)
    {
        var current = provider.CreateProtector(purpose);
        try { return (current.Unprotect(encrypted), null); }
        catch (CryptographicException) { }

        var oldPurpose = purpose.Replace("HDWebFramework", "MyShop", StringComparison.Ordinal);
        try
        {
            var value = provider.CreateProtector(oldPurpose).Unprotect(encrypted);
            return (value, current.Protect(value));
        }
        catch (CryptographicException) { }

        var configured = configuration["Hosting:DataProtectionKeyPath"];
        var keyPath = !string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(configured, environment.ContentRootPath)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASP.NET", "DataProtection-Keys");
        if (Directory.Exists(keyPath))
        {
            var oldRoot = environment.ContentRootPath.Replace("HDWebFramework", "MyShop", StringComparison.Ordinal);
            foreach (var identity in new[] { "MyShop", oldRoot, Path.TrimEndingDirectorySeparator(oldRoot) + Path.DirectorySeparatorChar }.Distinct())
            {
                var legacy = DataProtectionProvider.Create(new DirectoryInfo(keyPath), options => options.SetApplicationName(identity));
                try
                {
                    var value = legacy.CreateProtector(oldPurpose).Unprotect(encrypted);
                    return (value, current.Protect(value));
                }
                catch (CryptographicException) { }
            }
        }
        throw new CryptographicException("De opgeslagen geheime instelling kan niet worden ontsleuteld. Behoud de bestaande sleutelmap of voer de instelling opnieuw in.");
    }
}
