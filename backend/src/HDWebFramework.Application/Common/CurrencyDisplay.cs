namespace HDWebFramework.Application.Common;

public static class CurrencyDisplay
{
    public static string Symbol(string currency) => currency.Trim().ToUpperInvariant() switch
    {
        "EUR" => "€",
        "USD" => "$",
        "GBP" => "£",
        "SRD" => "SR$",
        var code => code,
    };
}
