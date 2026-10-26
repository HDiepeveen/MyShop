using System.Text;
using System.Text.RegularExpressions;

namespace HDWebFramework.Domain.Catalog;

public sealed record AttributeCode
{
    public const int MaximumLength = 64;
    private AttributeCode(string value) => Value = value;
    public string Value { get; }

    public static string Suggest(string? name)
    {
        var plain = Regex.Replace((name ?? string.Empty).Normalize(NormalizationForm.FormD), "[\\u0300-\\u036f]", string.Empty);
        var code = Regex.Replace(plain.Trim().ToLowerInvariant(), "[^a-z0-9_]+", "_").Trim('_');
        if (code.Length > 0 && !IsLowercaseAsciiLetter(code[0])) code = "kenmerk_" + code;
        return code.Length > MaximumLength ? code[..MaximumLength] : code;
    }
    public static AttributeCode Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length is 0 or > MaximumLength)
            throw new ArgumentException($"Attribute code must contain between 1 and {MaximumLength} characters.", nameof(value));

        if (!IsLowercaseAsciiLetter(value[0]) || value.Any(character =>
                !IsLowercaseAsciiLetter(character) && !char.IsAsciiDigit(character) && character != '_'))
            throw new ArgumentException("Attribute code must start with a lowercase ASCII letter and contain only lowercase ASCII letters, digits, or underscores.", nameof(value));

        return new AttributeCode(value);
    }

    public override string ToString() => Value;
    private static bool IsLowercaseAsciiLetter(char character) => character is >= 'a' and <= 'z';
}
