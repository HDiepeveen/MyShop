using System.Text.Json;
using System.Text.RegularExpressions;

namespace HDWebFramework.Domain.Catalog;

public sealed record RdwAttributeValue : AttributeValue
{
    private RdwAttributeValue(AttributeDefinitionId id, string json) : base(id)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("kenteken", out var plate))
            throw new ArgumentException("RDW-gegevens bevatten geen kenteken.");
        LicensePlate = NormalizePlate(plate.GetString() ?? "");
        Value = json;
    }

    public string Value { get; }
    public string LicensePlate { get; }
    public override AttributeDataType DataType => AttributeDataType.Rdw;
    public static RdwAttributeValue Create(AttributeDefinitionId id, string json) => new(id, json);

    public static string NormalizePlate(string value)
    {
        var plate = value.Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();
        if (!Regex.IsMatch(plate, "^[A-Z0-9]{6}$") || !plate.Any(char.IsLetter) || !plate.Any(char.IsDigit))
            throw new ArgumentException("Vul een Nederlands kenteken van zes letters en cijfers in.");
        return plate;
    }
}
