using System.Text.RegularExpressions;

namespace HDWebFramework.Application.Catalog.Seo;

public sealed record ShopAppearance(string PrimaryColor, string BackgroundColor, string PanelColor, string TextColor)
{
    public static ShopAppearance Default { get; } = new("#1d5543", "#f6f7f1", "#fffefa", "#223c33");

    public ShopAppearance Normalize() => new(Color(PrimaryColor), Color(BackgroundColor), Color(PanelColor), Color(TextColor));

    private static string Color(string value)
    {
        if (value is null || value.Length != 7 || !Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$"))
            throw new ArgumentException("Gebruik een kleur in de vorm #RRGGBB.");
        return value.ToLowerInvariant();
    }
}
public sealed record ShopAppearanceSettings(ShopAppearance Colors, Guid Revision, string? IconUrl = null, bool ShowManagementMenuWithoutLogin = false);
