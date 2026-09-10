namespace MyShop.Domain.Catalog;

public sealed class AttributeDefinition
{
    internal AttributeDefinition(AttributeDefinitionId id, AttributeCode code, string displayName,
        AttributeDataType dataType, bool isRequired, bool isFilterable, AttributeScope scope)
    {
        if (id == default)
            throw new ArgumentException("Attribute definition ID must not be empty.", nameof(id));

        Id = id;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        DisplayName = ValidateDisplayName(displayName);

        if (!Enum.IsDefined(dataType))
            throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Attribute data type is not supported.");
        if (!Enum.IsDefined(scope))
            throw new ArgumentOutOfRangeException(nameof(scope), scope, "Attribute scope is not supported.");

        DataType = dataType;
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        Scope = scope;
    }

    internal static AttributeDefinition Rehydrate(
        AttributeDefinitionId id,
        AttributeCode code,
        string displayName,
        AttributeDataType dataType,
        bool isRequired,
        bool isFilterable,
        AttributeScope scope) =>
        new(id, code, displayName, dataType, isRequired, isFilterable, scope);

    public AttributeDefinitionId Id { get; }
    public AttributeCode Code { get; }
    public string DisplayName { get; private set; }
    public AttributeDataType DataType { get; }
    public bool IsRequired { get; private set; }
    public bool IsFilterable { get; private set; }
    public AttributeScope Scope { get; }

    internal void Rename(string displayName) => DisplayName = ValidateDisplayName(displayName);
    internal void SetRequired(bool required) => IsRequired = required;
    internal void SetFilterable(bool filterable) => IsFilterable = filterable;

    private static string ValidateDisplayName(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Attribute display name must not be empty or whitespace.", nameof(displayName));
        return displayName;
    }
}
