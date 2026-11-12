using System.Text.RegularExpressions;

namespace HDWebFramework.Application.Catalog.Seo;

public sealed record ShopAppearance(string PrimaryColor, string BackgroundColor, string PanelColor, string TextColor)
{
    public static ShopAppearance Default { get; } = new("#1d5543", "#f6f7f1", "#fffefa", "#223c33");

    public string PhotoTextColor { get; init; } = "#dbeafe";
    public int AssortmentButtonOpacity { get; init; } = 10;

    public ShopAppearance Normalize()
    {
        if (AssortmentButtonOpacity is < 0 or > 100)
            throw new ArgumentException("Kies een knopdekking van 0 tot 100 procent.");

        return new(Color(PrimaryColor), Color(BackgroundColor), Color(PanelColor), Color(TextColor))
        {
            PhotoTextColor = Color(PhotoTextColor),
            AssortmentButtonOpacity = AssortmentButtonOpacity
        };
    }

    private static string Color(string value)
    {
        if (value is null || value.Length != 7 || !Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$"))
            throw new ArgumentException("Gebruik een kleur in de vorm #RRGGBB.");
        return value.ToLowerInvariant();
    }
}
public sealed record ShopAppearanceSettings(ShopAppearance Colors, Guid Revision, string? IconUrl = null, bool ShowManagementMenuWithoutLogin = false);
