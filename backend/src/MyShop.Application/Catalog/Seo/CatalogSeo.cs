using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MyShop.Application.Catalog.Seo;

public sealed record ShopSeoSettings(string Heading, string SeoTitle, Guid Revision, string ShopName, string WelcomeText, string Introduction);
public sealed record ProductSeoValues(string? SeoTitle, string? SeoDescription, string? WebAddress);
public sealed record ProductSeoInfo(Guid ProductId, string Name, string Description, string? ImageUrl,
    string ImageAlt, bool Published, ProductSeoValues Values, Guid Revision)
{
    public string ShopName { get; init; } = "MyShop";
    public string ResolvedTitle => Values.SeoTitle ?? Name + " · " + ShopName;
    public string ResolvedDescription => Values.SeoDescription ?? SeoText.Summary(Description);
    public string ResolvedAddress => Values.WebAddress ?? SeoText.AutomaticAddress(Name, ProductId);
}
public enum SeoFailure { NotFound, Conflict, AddressInUse }
public sealed record SeoResult(SeoFailure? Failure);
public interface ICatalogSeoStore
{
    Task<ShopSeoSettings> GetSettingsAsync(CancellationToken cancellationToken);
    Task<SeoResult> SaveSettingsAsync(ShopSeoSettings settings, CancellationToken cancellationToken);
    Task<ProductSeoInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken);
    Task<Guid?> ResolveProductAsync(string key, CancellationToken cancellationToken);
    Task<SeoResult> SaveProductAsync(Guid productId, ProductSeoValues values, Guid revision, CancellationToken cancellationToken);
}
public static class SeoText
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    { "betaling", "account", "inloggen", "registreren", "winkelmand", "e-mail-bevestigen", "wachtwoord-herstellen", "wachtwoord-vergeten" };
    public static string? Optional(string? value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (value.Length > limit || value.Any(c => char.IsControl(c) && !char.IsWhiteSpace(c)))
            throw new ArgumentException($"Gebruik maximaal {limit} tekens.");
        return Regex.Replace(value, @"\s+", " ");
    }
    public static string? Address(string? value)
    {
        value = Optional(value, 160)?.ToLowerInvariant();
        if (value is null) return null;
        if (!Regex.IsMatch(value, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || Reserved.Contains(value) || AutomaticId(value) is not null)
            throw new ArgumentException("Gebruik een uniek webadres met kleine letters, cijfers en streepjes, bijvoorbeeld opel-corsa-2014.");
        return value;
    }
    public static Guid? AutomaticId(string key) => key.Length > 37 && key[^37] == '-' && Guid.TryParseExact(key[^36..], "D", out var id) && id != Guid.Empty ? id : null;
    public static string AutomaticAddress(string name, Guid id)
    {
        var plain = string.Concat(name.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
        var prefix = Regex.Replace(plain.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (prefix.Length > 80) prefix = prefix[..80].TrimEnd('-');
        return (prefix.Length == 0 ? "product" : prefix) + "-" + id.ToString("D");
    }
    public static string Summary(string value)
    {
        value = Regex.Replace(value, @"\s+", " ").Trim();
        if (value.Length <= 200) return value;
        return value[..(char.IsHighSurrogate(value[199]) ? 199 : 200)].TrimEnd();
    }
}
public sealed class ManageSeo(ICatalogSeoStore store)
{
    public Task<ShopSeoSettings> GetSettingsAsync(CancellationToken cancellationToken) => store.GetSettingsAsync(cancellationToken);
    public Task<ProductSeoInfo?> GetProductAsync(Guid id, CancellationToken cancellationToken) => store.GetProductAsync(id, cancellationToken);
    public Task<SeoResult> SaveSettingsAsync(string heading, string title, Guid revision, CancellationToken cancellationToken,
        string shopName, string welcomeText, string introduction)
    {
        var settings = new ShopSeoSettings(SeoText.Optional(heading, 200) ?? throw new ArgumentException("Vul een koptekst in."),
            SeoText.Optional(title, 200) ?? throw new ArgumentException("Vul een SEO-titel in."), revision,
            SeoText.Optional(shopName, 100) ?? throw new ArgumentException("Vul een webshopnaam in."),
            SeoText.Optional(welcomeText, 200) ?? throw new ArgumentException("Vul een welkomsttekst in."),
            SeoText.Optional(introduction, 1000) ?? throw new ArgumentException("Vul een introductietekst in."));
        if (revision == Guid.Empty) throw new ArgumentException("Vernieuw de instellingen.");
        return store.SaveSettingsAsync(settings, cancellationToken);
    }
    public Task<SeoResult> SaveProductAsync(Guid id, ProductSeoValues values, Guid revision, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw new ArgumentException("Product ontbreekt.");
        return store.SaveProductAsync(id, new(SeoText.Optional(values.SeoTitle, 200), SeoText.Optional(values.SeoDescription, 500), SeoText.Address(values.WebAddress)), revision, cancellationToken);
    }
}
