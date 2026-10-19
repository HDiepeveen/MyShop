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
            new(null, "Openingstijden\n8:00 - 17:00"),
            new("Inloggen", "Geen beheerpagina."),
        ]) { Place = "Zwolle" }.Normalize();
        Assert.Equal(["apk-zwolle", "apk-zwolle-2", "pagina-zwolle", "inloggen-zwolle"], settings.Items.Select(x => x.WebAddress));
        Assert.Equal("Openingstijden\n8:00 - 17:00", settings.Items[2].Description);
        Assert.Null(settings.Items[2].Title);
    }

    [Fact]
    public void Keeps_published_addresses_stable_when_title_or_place_changes()
    {
        var settings = new HomePageSettings(true, "Welkom", [new("Nieuwe titel", "Nieuwe inhoud.") { Place = "Nieuwe plaats", WebAddress = "apk-zwolle" }]) { Place = "Nieuwe plaats" }.Normalize();
        Assert.Equal("apk-zwolle", settings.Items[0].WebAddress);
        Assert.Equal("Nieuwe plaats", settings.Items[0].Place);
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
