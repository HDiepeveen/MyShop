using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HDWebFramework.Application.Catalog.Seo;

public sealed record ShopSeoSettings(string Heading, string SeoTitle, Guid Revision, string ShopName, string WelcomeText, string Introduction, string FooterText, CompanyPage Company) { public string Currency { get; init; } = "EUR"; public string? SeoDescription { get; init; } public string? HomeImageUrl { get; init; } public string? AssortmentImageUrl { get; init; } public string? AssortmentImageMode { get; init; } public bool ShowHeaderPhone { get; init; } = true; public bool ShowHeaderWhatsapp { get; init; } = true; public string? HeaderText { get; init; } public HomePageSettings Home { get; init; } = HomePageSettings.Default; public ShopAppearance Appearance { get; init; } = ShopAppearance.Default; public string? IconUrl { get; init; } public bool ShowManagementMenuWithoutLogin { get; init; } }
public sealed record HomePageItem(string? Title, string Description)
{
    public Guid PageId { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageMode { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }

    public string? Place { get; init; }
    public string? WebAddress { get; init; }
}
public sealed record HomePageSettings(bool Enabled, string Heading, IReadOnlyList<HomePageItem> Items)
{
    public bool ContactEnabled { get; init; } = true;
    public IReadOnlyList<string> MenuOrder { get; init; } = Array.Empty<string>();
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }

    public string? ImageMode { get; init; }
    public string? WelcomeText { get; init; }
    public string? ImageUrl { get; init; }
    public string? Place { get; init; }
    public string? Description { get; init; }
    public static HomePageSettings Default => new(false, "Welkom", Array.Empty<HomePageItem>());
    public HomePageSettings Normalize(bool forSaving = false)
    {
        if (MenuOrder is null || MenuOrder.Count > 100 || MenuOrder.Any(key => string.IsNullOrWhiteSpace(key) || key.Length > 200) || MenuOrder.Distinct(StringComparer.Ordinal).Count() != MenuOrder.Count) throw new ArgumentException("Gebruik een geldige volgorde voor de menubalk.");
        if (Items is null || Items.Count > 20) throw new ArgumentException("Gebruik maximaal 20 pagina’s.");
        return this with {
            SeoTitle = SeoText.Optional(SeoTitle, 200),
            SeoDescription = SeoText.Optional(SeoDescription, 500),
            ImageMode = PageBackground.Mode(ImageMode),
            Heading = SeoText.Optional(Heading, 200) ?? throw new ArgumentException("Vul een koptekst voor de homepagina in."),
            Place = SeoText.Optional(Place, 100),
            Description = string.IsNullOrWhiteSpace(Description) ? null : NormalizeDescription(Description),
            Items = ResolveItems(Items, SeoText.Optional(Place, 100), forSaving)
        };
    }
    private static IReadOnlyList<HomePageItem> ResolveItems(IReadOnlyList<HomePageItem> items, string? place, bool forSaving)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<HomePageItem>();
        var ids = new HashSet<Guid>();
        foreach (var item in items)
        {
            if (item is null) throw new ArgumentException("Vul de pagina in.");
            // Older website pages have no ID. Resolve a stable, distinct ID on reads as well as saves.
            var id = item.PageId == Guid.Empty
                ? new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"HDWebFramework.HomePage:{result.Count}:{item.WebAddress ?? item.Title ?? string.Empty}"))[..16])
                : item.PageId;
            if (id == PageBackground.ContactId || id == PageBackground.AssortmentId || (id != Guid.Empty && !ids.Add(id))) throw new ArgumentException("Elke pagina moet een eigen identiteit hebben.");
            var title = SeoText.Optional(item.Title, 200);
            if (forSaving && title is null) throw new ArgumentException("Vul voor elke extra pagina een titel in.");
            var address = forSaving || item.WebAddress is null ? null : ServicePageAddress.Validate(item.WebAddress);
            if (address is null)
            {
                var stem = ServicePageAddress.Generate(title ?? "pagina");
                address = stem;
                for (var suffix = 2; used.Contains(address); suffix++) address = stem + "-" + suffix;
            }
            if (!used.Add(address)) throw new ArgumentException("Elke pagina moet een uniek webadres hebben.");
            result.Add(new(title, NormalizeDescription(item.Description)) { PageId = id, ImageUrl = item.ImageUrl, ImageMode = PageBackground.Mode(item.ImageMode), Place = place, WebAddress = address, SeoTitle = SeoText.Optional(item.SeoTitle, 200), SeoDescription = SeoText.Optional(item.SeoDescription, 500) });
        }
        return result;
    }
    private static string NormalizeDescription(string? value)
    {
        value = value?.Trim().Replace("\r\n", "\n");
        if (string.IsNullOrWhiteSpace(value) || value.Length > 4000 || value.Any(c => char.IsControl(c) && c is not ('\n' or '\t')))
            throw new ArgumentException("Vul een omschrijving van maximaal 4000 tekens in.");
        return value;
    }
}
public static class ServicePageAddress
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    { "algemene-voorwaarden", "privacyverklaring", "cookiebeleid", "legal", "api", "winkel", "assortiment", "contact", "beheer", "home", "inloggen", "geen-toegang", "producten", "producttypen", "categorieen", "bestellingen", "klanten", "instellingen", "account", "assets", "robots-txt", "sitemap-xml" };
    public static string Generate(string title)
    {
        var plain = string.Concat(title.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
        var stem = Regex.Replace(plain.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (stem.Length > 140) stem = stem[..140].TrimEnd('-');
        if (stem.Length == 0) stem = "pagina";
        return Reserved.Contains(stem) ? stem + "-pagina" : stem;
    }
    public static string Validate(string address)
    {
        if (address.Length > 160 || !Regex.IsMatch(address, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || Reserved.Contains(address))
            throw new ArgumentException("Gebruik een geldig, uniek pagina-adres.");
        return address;
    }
}
public sealed record CompanyPage(string Heading, string? Name, string? Description, string? Address, string? Email, string? Phone, string? OpeningHours)
{
    public string? ImageUrl { get; init; }
    public string? ImageMode { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
}
public sealed record ProductSeoValues(string? SeoTitle, string? SeoDescription, string? WebAddress);
public sealed record ProductTypeHeadings(Guid ProductTypeId, string? AboutHeading, string? AttributesHeading, Guid Revision);
public sealed record ProductSeoInfo(Guid ProductId, string Name, string Description, string? ImageUrl,
    string ImageAlt, bool Published, ProductSeoValues Values, Guid Revision)
{
    public string ShopName { get; init; } = "HD Web Framework";
    public string ResolvedTitle => Values.SeoTitle ?? Name + " · " + ShopName;
    public string ResolvedAboutHeading { get; init; } = "Over dit product";
    public string ResolvedAttributesHeading { get; init; } = "Productkenmerken";
    public string ResolvedDescription => Values.SeoDescription ?? SeoText.Summary(Description);
    public string ResolvedAddress => Values.WebAddress ?? SeoText.AutomaticAddress(Name, ProductId);
}
public enum SeoFailure { NotFound, Conflict, AddressInUse }
public sealed record SeoResult(SeoFailure? Failure);
public interface ICatalogSeoStore
{
    Task<SeoResult> SavePageImageAsync(Guid pageId, HDWebFramework.Application.Catalog.Abstractions.ProductImageUpload? image, Guid revision, CancellationToken ct);
    Task<HDWebFramework.Application.Catalog.Abstractions.ProductImageContent?> GetPageImageAsync(Guid pageId, CancellationToken ct);
    Task<SeoResult> SaveHomeImageAsync(HDWebFramework.Application.Catalog.Abstractions.ProductImageUpload? image, Guid revision, CancellationToken ct);
    Task<HDWebFramework.Application.Catalog.Abstractions.ProductImageContent?> GetHomeImageAsync(CancellationToken ct);
    Task<SeoResult> SaveIconAsync(HDWebFramework.Application.Catalog.Abstractions.ProductImageUpload? icon, Guid revision, CancellationToken ct);
    Task<HDWebFramework.Application.Catalog.Abstractions.ProductImageContent?> GetIconAsync(CancellationToken ct);
    Task<ProductTypeHeadings?> GetTypeHeadingsAsync(Guid id, CancellationToken cancellationToken);
    Task<SeoResult> SaveTypeHeadingsAsync(ProductTypeHeadings headings, CancellationToken cancellationToken);
    Task<SeoResult> SaveAppearanceAsync(ShopAppearanceSettings settings, CancellationToken cancellationToken);
    Task<ShopSeoSettings> GetSettingsAsync(CancellationToken cancellationToken);
    Task<SeoResult> SaveSettingsAsync(ShopSeoSettings settings, CancellationToken cancellationToken);
    Task<ProductSeoInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken);
    Task<Guid?> ResolveProductAsync(string key, CancellationToken cancellationToken);
    Task<SeoResult> SaveProductAsync(Guid productId, ProductSeoValues values, Guid revision, CancellationToken cancellationToken);
}
public static class SeoText
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    { "home", "betaling", "account", "inloggen", "registreren", "winkelmand", "e-mail-bevestigen", "wachtwoord-herstellen", "wachtwoord-vergeten" };
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
public static class PageBackground
{
    public static readonly Guid AssortmentId = Guid.Parse("d2f06ded-68a2-4936-9880-327d616d1a42");
    public static readonly Guid ContactId = Guid.Parse("ac8107f2-a136-4b4f-9e85-b70f14860701");
    public static string? Mode(string? value) => value switch { null or "" => null, "stretch" => "stretch", "cover" => "cover", _ => throw new ArgumentException("Kies een geldige afbeeldingsweergave.") };
}
public sealed class ManageSeo(ICatalogSeoStore store)
{
    public Task<HDWebFramework.Application.Catalog.Abstractions.ProductImageContent?> GetPageImageAsync(Guid pageId, CancellationToken ct) => store.GetPageImageAsync(pageId, ct);
    public Task<SeoResult> SavePageImageAsync(Guid pageId, byte[]? bytes, string fileName, Guid revision, CancellationToken ct)
    {
        if (revision == Guid.Empty || pageId == Guid.Empty) throw new ArgumentException("Sla de pagina eerst op.");
        if (bytes?.Length > MaximumHomeImageBytes) throw new ArgumentException("Kies een afbeelding van maximaal 5 MB.");
        var image = bytes is null ? null : HDWebFramework.Application.Catalog.Abstractions.ProductImageUpload.Create(bytes, "Pagina-achtergrond", fileName);
        return store.SavePageImageAsync(pageId, image, revision, ct);
    }
    public const int MaximumHomeImageBytes = 5 * 1024 * 1024;
    public Task<HDWebFramework.Application.Catalog.Abstractions.ProductImageContent?> GetHomeImageAsync(CancellationToken ct) => store.GetHomeImageAsync(ct);
    public Task<SeoResult> SaveHomeImageAsync(byte[]? bytes, string fileName, Guid revision, CancellationToken ct)
    {
        if (revision == Guid.Empty) throw new ArgumentException("Vernieuw de instellingen.");
        if (bytes?.Length > MaximumHomeImageBytes) throw new ArgumentException("Kies een afbeelding van maximaal 5 MB.");
        var image = bytes is null ? null : HDWebFramework.Application.Catalog.Abstractions.ProductImageUpload.Create(bytes, "Homepagina", fileName);
        return store.SaveHomeImageAsync(image, revision, ct);
    }
    public const int MaximumIconBytes = 1024 * 1024;
    public Task<HDWebFramework.Application.Catalog.Abstractions.ProductImageContent?> GetIconAsync(CancellationToken ct) => store.GetIconAsync(ct);
    public Task<SeoResult> SaveIconAsync(byte[]? bytes, string fileName, Guid revision, CancellationToken ct)
    {
        if (revision == Guid.Empty) throw new ArgumentException("Vernieuw de instellingen.");
        if (bytes?.Length > MaximumIconBytes) throw new ArgumentException("Kies een icoon van maximaal 1 MB.");
        var icon = bytes is null ? null : HDWebFramework.Application.Catalog.Abstractions.ProductImageUpload.Create(bytes, "Webshopicoon", fileName);
        return store.SaveIconAsync(icon, revision, ct);
    }
    public Task<ProductTypeHeadings?> GetTypeHeadingsAsync(Guid id, CancellationToken ct) => store.GetTypeHeadingsAsync(id, ct);
    public Task<SeoResult> SaveTypeHeadingsAsync(ProductTypeHeadings headings, CancellationToken ct)
    {
        if (headings.ProductTypeId == Guid.Empty) throw new ArgumentException("Producttype ontbreekt.");
        return store.SaveTypeHeadingsAsync(headings with { AboutHeading = SeoText.Optional(headings.AboutHeading, 200),
            AttributesHeading = SeoText.Optional(headings.AttributesHeading, 200) }, ct);
    }
    public Task<SeoResult> SaveAppearanceAsync(ShopAppearanceSettings value, CancellationToken ct)
    {
        if (value.Revision == Guid.Empty) throw new ArgumentException("Vernieuw de instellingen.");
        if (value.Colors is null) throw new ArgumentException("Kies een kleurenschema.");
        return store.SaveAppearanceAsync(value with { Colors = value.Colors.Normalize() }, ct);
    }
    public Task<ShopSeoSettings> GetSettingsAsync(CancellationToken cancellationToken) => store.GetSettingsAsync(cancellationToken);
    public Task<ProductSeoInfo?> GetProductAsync(Guid id, CancellationToken cancellationToken) => store.GetProductAsync(id, cancellationToken);
    public Task<SeoResult> SaveSettingsAsync(ShopSeoSettings value, CancellationToken ct)
    {
        if (value.Revision == Guid.Empty) throw new ArgumentException("Vernieuw de instellingen.");
        if (value.Company is null) throw new ArgumentException("Vul de bedrijfsinformatie in.");
        var company = value.Company;
        var email = SeoText.Optional(company.Email, 254);
        if (email is not null && (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed)
            || !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Vul een geldig e-mailadres in.");
        var settings = value with
        {
            Home = (value.Home ?? throw new ArgumentException("Vul de homepagina in.")).Normalize(forSaving: true),
            Heading = SeoText.Optional(value.Heading, 200) ?? throw new ArgumentException("Vul een koptekst in."),
            SeoTitle = SeoText.Optional(value.SeoTitle, 200) ?? throw new ArgumentException("Vul een SEO-titel in."),
            ShopName = SeoText.Optional(value.ShopName, 100) ?? throw new ArgumentException("Vul een webshopnaam in."),
            WelcomeText = SeoText.Optional(value.WelcomeText, 200) ?? throw new ArgumentException("Vul een welkomsttekst in."),
            Introduction = SeoText.Optional(value.Introduction, 1000) ?? throw new ArgumentException("Vul een introductietekst in."),
            Currency = HDWebFramework.Domain.Catalog.Money.Create(0, value.Currency).Currency,
            FooterText = SeoText.Optional(value.FooterText, 200) ?? "",
            HeaderText = SeoText.Optional(value.HeaderText, 200),
            AssortmentImageMode = PageBackground.Mode(value.AssortmentImageMode),
            SeoDescription = SeoText.Optional(value.SeoDescription, 500),
            Company = company with
            {
                ImageMode = PageBackground.Mode(company.ImageMode),
                Heading = SeoText.Optional(company.Heading, 200) ?? throw new ArgumentException("Vul een koptekst voor de bedrijfspagina in."),
                SeoTitle = SeoText.Optional(company.SeoTitle, 200), SeoDescription = SeoText.Optional(company.SeoDescription, 500),
                Name = SeoText.Optional(company.Name, 200), Description = Multiline(company.Description, 4000),
                Address = Multiline(company.Address, 500), Email = email,
                Phone = SeoText.Optional(company.Phone, 100), OpeningHours = Multiline(company.OpeningHours, 1000)
            }
        };
        return store.SaveSettingsAsync(settings, ct);
    }
    private static string? Multiline(string? value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim().Replace("\r\n", "\n");
        if (value.Length > limit || value.Any(c => char.IsControl(c) && c is not ('\n' or '\t')))
            throw new ArgumentException($"Gebruik maximaal {limit} tekens.");
        return value;
    }
    public Task<SeoResult> SaveProductAsync(Guid id, ProductSeoValues values, Guid revision, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw new ArgumentException("Product ontbreekt.");
        return store.SaveProductAsync(id, new(SeoText.Optional(values.SeoTitle, 200), SeoText.Optional(values.SeoDescription, 500), SeoText.Address(values.WebAddress)), revision, cancellationToken);
    }
}
