using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class AttributeValuePersistenceMapper
{
    internal static AttributeValue ToDomain(ProductAttributeValuePersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(persistence);
        if (persistence.ProductId == Guid.Empty)
            throw InvalidState(persistence.AttributeDefinitionId, persistence.DataType, "Product ID must not be empty.");

        var children = persistence.MultiChoiceValues
            ?? throw InvalidState(persistence.AttributeDefinitionId, persistence.DataType,
                "Multi-choice collection must not be null.");

        return ToDomain(
            persistence.AttributeDefinitionId,
            persistence.DataType,
            persistence.TextValue,
            persistence.IntegerValue,
            persistence.DecimalCoefficient,
            persistence.DecimalScale,
            persistence.BooleanValue,
            persistence.DateValue,
            persistence.ChoiceValue,
            children.Select(child =>
            {
                if (child is null)
                    throw InvalidState(persistence.AttributeDefinitionId, persistence.DataType,
                        "Multi-choice collection must not contain null children.");
                if (child.ProductId != persistence.ProductId)
                    throw InvalidState(persistence.AttributeDefinitionId, persistence.DataType,
                        "Multi-choice child Product ID does not match its parent.");
                return new MultiChoiceEntry(child.AttributeDefinitionId, child.Ordinal, child.Value);
            }).ToList());
    }

    internal static AttributeValue ToDomain(ProductVariantAttributeValuePersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(persistence);
        if (persistence.ProductVariantId == Guid.Empty)
            throw InvalidState(persistence.AttributeDefinitionId, persistence.DataType,
                "Product variant ID must not be empty.");

        var children = persistence.MultiChoiceValues
            ?? throw InvalidState(persistence.AttributeDefinitionId, persistence.DataType,
                "Multi-choice collection must not be null.");

        return ToDomain(
            persistence.AttributeDefinitionId,
            persistence.DataType,
            persistence.TextValue,
            persistence.IntegerValue,
            persistence.DecimalCoefficient,
            persistence.DecimalScale,
            persistence.BooleanValue,
            persistence.DateValue,
            persistence.ChoiceValue,
            children.Select(child =>
            {
                if (child is null)
                    throw InvalidState(persistence.AttributeDefinitionId, persistence.DataType,
                        "Multi-choice collection must not contain null children.");
                if (child.ProductVariantId != persistence.ProductVariantId)
                    throw InvalidState(persistence.AttributeDefinitionId, persistence.DataType,
                        "Multi-choice child Product variant ID does not match its parent.");
                return new MultiChoiceEntry(child.AttributeDefinitionId, child.Ordinal, child.Value);
            }).ToList());
    }

    private static AttributeValue ToDomain(
        Guid attributeDefinitionId,
        AttributeDataType dataType,
        string? textValue,
        long? integerValue,
        decimal? decimalCoefficient,
        byte? decimalScale,
        bool? booleanValue,
        DateOnly? dateValue,
        string? choiceValue,
        IReadOnlyCollection<MultiChoiceEntry> multiChoiceValues)
    {
        var id = AttributeDefinitionId.From(attributeDefinitionId);
        if (!Enum.IsDefined(dataType))
            throw InvalidState(attributeDefinitionId, dataType, "Attribute data type is undefined.");

        var scalarCount =
            (textValue is null ? 0 : 1) +
            (integerValue is null ? 0 : 1) +
            (decimalCoefficient is null ? 0 : 1) +
            (decimalScale is null ? 0 : 1) +
            (booleanValue is null ? 0 : 1) +
            (dateValue is null ? 0 : 1) +
            (choiceValue is null ? 0 : 1);

        if (dataType != AttributeDataType.MultiChoice && multiChoiceValues.Count != 0)
            throw InvalidState(attributeDefinitionId, dataType,
                "Scalar attribute value must not contain multi-choice children.");

        return dataType switch
        {
            AttributeDataType.Text when textValue is not null && scalarCount == 1 =>
                TextAttributeValue.Create(id, textValue),
            AttributeDataType.Integer when integerValue is not null && scalarCount == 1 =>
                IntegerAttributeValue.Create(id, integerValue.Value),
            AttributeDataType.Decimal when decimalCoefficient is not null && decimalScale is not null && scalarCount == 2 =>
                DecimalAttributeValue.Create(id, DecimalAttributeValuePersistenceConverter.ToDomain(
                    decimalCoefficient.Value, decimalScale.Value)),
            AttributeDataType.Boolean when booleanValue is not null && scalarCount == 1 =>
                BooleanAttributeValue.Create(id, booleanValue.Value),
            AttributeDataType.Date when dateValue is not null && scalarCount == 1 =>
                DateAttributeValue.Create(id, dateValue.Value),
            AttributeDataType.Choice when choiceValue is not null && scalarCount == 1 =>
                ChoiceAttributeValue.Create(id, ChoiceValue.Create(choiceValue)),
            AttributeDataType.MultiChoice when scalarCount == 0 =>
                CreateMultiChoice(id, attributeDefinitionId, dataType, multiChoiceValues),
            _ => throw InvalidState(attributeDefinitionId, dataType,
                "Persisted payload does not match its attribute data type.")
        };
    }

    private static MultiChoiceAttributeValue CreateMultiChoice(
        AttributeDefinitionId id,
        Guid attributeDefinitionId,
        AttributeDataType dataType,
        IReadOnlyCollection<MultiChoiceEntry> entries)
    {
        if (entries.Count == 0)
            throw InvalidState(attributeDefinitionId, dataType,
                "Multi-choice attribute value must contain at least one child.");
        if (entries.Any(entry => entry.AttributeDefinitionId != attributeDefinitionId))
            throw InvalidState(attributeDefinitionId, dataType,
                "Multi-choice child Attribute definition ID does not match its parent.");
        if (entries.GroupBy(entry => entry.Ordinal).Any(group => group.Count() > 1))
            throw InvalidState(attributeDefinitionId, dataType,
                "Multi-choice child ordinals must be distinct.");

        var values = entries
            .OrderBy(entry => entry.Ordinal)
            .Select(entry => ChoiceValue.Create(entry.Value))
            .ToList();

        return MultiChoiceAttributeValue.Create(id, values);
    }

    private static InvalidOperationException InvalidState(
        Guid attributeDefinitionId,
        AttributeDataType dataType,
        string message) =>
        new($"Invalid persisted attribute value '{attributeDefinitionId}' with data type '{dataType}': {message}");

    private readonly record struct MultiChoiceEntry(
        Guid AttributeDefinitionId,
        int Ordinal,
        string Value);
}
