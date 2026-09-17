using System.Globalization;
using System.Text.Json;
using MyShop.Application.Catalog;
using MyShop.Domain.Catalog;

namespace MyShop.Api.Catalog.Products;

public sealed record AttributeValueRequest(
    AttributeDataType DataType,
    JsonElement Value);

internal static class AttributeValueRequestMapper
{
    public static CatalogAttributeValueInput Map(AttributeValueRequest request) =>
        request.DataType switch
        {
            AttributeDataType.Text => new TextAttributeValueInput(ReadString(request.Value, "text")),
            AttributeDataType.Integer => new IntegerAttributeValueInput(ReadInteger(request.Value)),
            AttributeDataType.Decimal => new DecimalAttributeValueInput(ReadDecimal(request.Value)),
            AttributeDataType.Boolean => new BooleanAttributeValueInput(ReadBoolean(request.Value)),
            AttributeDataType.Date => new DateAttributeValueInput(ReadDate(request.Value)),
            AttributeDataType.Choice => new ChoiceAttributeValueInput(ReadString(request.Value, "choice")),
            AttributeDataType.MultiChoice => new MultiChoiceAttributeValueInput(ReadStrings(request.Value)),
            _ => throw new ArgumentException(
                $"Attribute data type '{request.DataType}' is not supported.", nameof(request))
        };

    private static string ReadString(JsonElement value, string description)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw InvalidValue(description);
        return value.GetString()!;
    }

    private static long ReadInteger(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var result))
            throw InvalidValue("integer");
        return result;
    }

    private static decimal ReadDecimal(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var result))
            throw InvalidValue("decimal");
        return result;
    }

    private static bool ReadBoolean(JsonElement value)
    {
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw InvalidValue("boolean");
        return value.GetBoolean();
    }

    private static DateOnly ReadDate(JsonElement value)
    {
        var text = ReadString(value, "date");
        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
            throw InvalidValue("date in yyyy-MM-dd format");
        return result;
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
            throw InvalidValue("array of choices");

        var result = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                throw InvalidValue("array of choices");
            result.Add(item.GetString()!);
        }
        return result;
    }

    private static ArgumentException InvalidValue(string expected) =>
        new($"Value must be a valid {expected}.", "request");
}
