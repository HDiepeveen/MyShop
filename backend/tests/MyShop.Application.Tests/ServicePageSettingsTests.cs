using MyShop.Application.Catalog.Seo;

namespace MyShop.Application.Tests;

public sealed class ServicePageSettingsTests
{
    [Fact]
    public void Generates_readable_unique_addresses_for_existing_blocks_and_retains_content()
    {
        var settings = new HomePageSettings(true, "Welkom", [
            new("APK", "Keuring op afspraak.") { Place = "Zwolle" },
            new("APK", "Tweede dienst.") { Place = "Zwolle" },
            new("Openingstijden", "Openingstijden\n8:00 - 17:00"),
            new("Inloggen", "Geen beheerpagina."),
        ]) { Place = "Zwolle" }.Normalize(forSaving: true);
        Assert.Equal(["apk", "apk-2", "openingstijden", "inloggen-pagina"], settings.Items.Select(x => x.WebAddress));
        Assert.Equal("Openingstijden\n8:00 - 17:00", settings.Items[2].Description);
        Assert.Equal("Openingstijden", settings.Items[2].Title);
    }

    [Fact]
    public void Regenerates_addresses_from_the_title_without_the_place()
    {
        var settings = new HomePageSettings(true, "Welkom", [new("Nieuwe titel", "Nieuwe inhoud.") { Place = "Nieuwe plaats", WebAddress = "apk-zwolle" }]) { Place = "Nieuwe plaats" }.Normalize(forSaving: true);
        Assert.Equal("nieuwe-titel", settings.Items[0].WebAddress);
        Assert.Equal("Nieuwe plaats", settings.Items[0].Place);
    }

    [Fact]
    public void Changing_the_website_place_does_not_change_the_address()
    {
        var original = new HomePageSettings(true, "Welkom", [new("APK", "Keuring.")]) { Place = "Zwolle" }.Normalize(forSaving: true);
        var changed = (original with { Place = "Utrecht" }).Normalize(forSaving: true);
        Assert.Equal("apk", changed.Items[0].WebAddress);
        Assert.Equal("Utrecht", changed.Items[0].Place);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Existing_untitled_pages_remain_readable_but_cannot_be_saved(string? title)
    {
        var legacy = new HomePageSettings(true, "Welkom", [new(title, "Bestaande tekst.") { WebAddress = "pagina-zwolle" }]);
        Assert.Equal("pagina-zwolle", legacy.Normalize().Items[0].WebAddress);
        Assert.Throws<ArgumentException>(() => legacy.Normalize(forSaving: true));
    }

    [Fact]
    public void Reordering_or_removing_duplicate_titles_keeps_generated_addresses_unique()
    {
        var settings = new HomePageSettings(true, "Welkom", [new("APK", "Een.") { WebAddress = "oud" }, new("ÁPK", "Twee.") { WebAddress = "ook-oud" }]).Normalize(forSaving: true);
        Assert.Equal(["apk", "apk-2"], settings.Items.Select(item => item.WebAddress));
        var remaining = (settings with { Items = [settings.Items[1]] }).Normalize(forSaving: true);
        Assert.Equal("apk", remaining.Items[0].WebAddress);
    }

    [Theory]
    [InlineData("beheer")]
    [InlineData("winkel")]
    [InlineData("api")]
    [InlineData("../inloggen")]
    [InlineData("https://evil.test")]
    public void Rejects_addresses_that_overlap_core_routes_or_escape_the_site(string address)
    {
        Assert.Throws<ArgumentException>(() => new HomePageSettings(true, "Welkom", [new("Dienst", "Inhoud.") { WebAddress = address }]).Normalize());
    }
}
