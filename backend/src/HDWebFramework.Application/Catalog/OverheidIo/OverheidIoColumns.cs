using HDWebFramework.Application.Catalog.Rdw;
namespace HDWebFramework.Application.Catalog.OverheidIo;

public static class OverheidIoColumns
{
    public static readonly IReadOnlyList<RdwColumn> All = Build();
    public static readonly string[] Default = ["kenteken", "merk", "handelsbenaming", "voertuigsoort", "eerste_kleur",
        "datum_eerste_toelating", "vervaldatum_apk", "aantal_zitplaatsen", "aantal_deuren", "massa_ledig_voertuig",
        "cilinderinhoud", "aantal_cilinders", "tellerstandoordeel", "brandstof.brandstof_omschrijving"];
    private static IReadOnlyList<RdwColumn> Build()
    {
        var fuelIndex = RdwColumns.All.ToList().FindIndex(c => c.Key == "brandstof_omschrijving");
        var main = RdwColumns.All.Take(fuelIndex).Where(c => !c.Key.StartsWith("api_") && !c.Key.EndsWith("_dt"))
            .Select(c => c with { Type = c.Key.StartsWith("datum_") || c.Key.StartsWith("vervaldatum") ? "calendar_date" : c.Type }).ToList();
        main.AddRange(new RdwColumn[] {
            new("kentekenplaat", "Kentekenplaat", "text"), new("inrichting", "Inrichting", "text"),
            new("sidecode", "Sidecode kenteken", "number"), new("tenaamstellen_mogelijk", "Tenaamstellen mogelijk", "text"),
            new("hoogte_voertuig", "Hoogte voertuig", "number"),
            new("brandstof.brandstof_omschrijving", "Brandstof", "text")
        });
        main.AddRange(RdwColumns.All.Skip(fuelIndex + 1).Select(c => c with { Key = "brandstof." + c.Key }));
        main.AddRange(new RdwColumn[] {
            new("brandstof.brandstofverbruik_stad", "Brandstofverbruik stad", "number"),
            new("brandstof.brandstofverbruik_buiten_de_stad", "Brandstofverbruik buiten de stad", "number"),
            new("brandstof.emissieklasse", "Emissieklasse", "text"),
            new("carrosserie.carrosserie_volgnummer", "Carrosserie volgnummer", "number"),
            new("carrosserie.carrosserietype", "Carrosserietype", "text"),
            new("carrosserie.type_carrosserie_europese_omschrijving", "Carrosserie Europese omschrijving", "text"),
            new("as.as_nummer", "Asnummer", "number"), new("as.plaatscode_as", "Plaatscode as", "text"),
            new("as.aantal_assen", "Aantal assen", "number"), new("as.spoorbreedte", "Spoorbreedte", "number"),
            new("as.wettelijk_toegestane_maximum_aslast", "Wettelijk toegestane maximum aslast", "number"),
            new("as.technisch_toegestane_maximum_aslast", "Technisch toegestane maximum aslast", "number")
        });
        return main.DistinctBy(c => c.Key).ToArray();
    }
}
